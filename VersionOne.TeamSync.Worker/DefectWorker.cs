using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker
{
    public class DefectWorker : IAsyncWorker
    {
        private const string PluralAsset = "defects";
        private const string CreatedFromV1Comment = "Created from VersionOne Work Item {0} in Project {1}";
        private const string V1AssetDetailWebLinkUrl = "{0}assetdetail.v1?Number={1}";
        private const string V1AssetDetailWebLinkTitle = "VersionOne Defect ({0})";

        private readonly IV1 _v1;
        private readonly ILog _log;

        public DefectWorker(IV1 v1, ILog log)
        {
            _v1 = v1;
            _log = log;
        }

        public async Task DoWork(V1JiraInfo jiraInfo)
        {
            _log.Trace("Defect sync started...");
            var allJiraBugs = jiraInfo.JiraInstance.GetBugsInProject(jiraInfo.JiraKey).issues;
            var allV1Defects = await _v1.GetDefectsWithJiraReference(jiraInfo.V1ProjectId);

            UpdateDefects(jiraInfo, allJiraBugs, allV1Defects);
            CreateDefects(jiraInfo, allJiraBugs, allV1Defects);
            DeleteV1Defects(jiraInfo, allJiraBugs, allV1Defects);

            _log.Trace("Defect sync stopped...");
        }

        public async void UpdateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraBugs, List<Defect> allV1Defects)
        {
            _log.Trace("Updating defects started");
            var updatedDefects = 0;
            var closedDefects = 0;

            var existingBugs =
                allJiraBugs.Where(bug =>
                {
                    return allV1Defects.Any(defect => bug.Fields.Labels.Contains(defect.Number));
                }).ToList();

            if (existingBugs.Any())
                _log.DebugFormat("Found {0} defects to check for update", existingBugs.Count);

            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            existingBugs.ForEach(existingJDefect =>
            {
                var defectToUpdate = allV1Defects.Single(defect => existingJDefect.Fields.Labels.Contains(defect.Number));

                var returnValue = UpdateDefectFromJiraToV1(jiraInfo, existingJDefect, defectToUpdate, assignedEpics);
                //checking if was an update or close
                switch (returnValue.Result)
                {
                    case 1:
                        updatedDefects++;
                        break;
                    case 2:
                        closedDefects++;
                        break;
                }
            });

            if (updatedDefects > 0)
                _log.InfoUpdated(updatedDefects, PluralAsset);
            if (closedDefects > 0)
                _log.InfoClosed(closedDefects, PluralAsset);
            _log.TraceUpdateFinished(PluralAsset);
        }

        public async Task<int> UpdateDefectFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Defect defect, List<Epic> assignedEpics)
        {
            var defectUpdatedClosed = 0;

            //need to reopen a defect first before we can update it
            if (issue.Fields.Status != null && !issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && defect.AssetState == "128")
            {
                await _v1.ReOpenDefect(defect.ID);
                _log.DebugFormat("Reopened V1 defect {0}", defect.Number);
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;
            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;
            var update = issue.ToV1Defect(jiraInfo.V1ProjectId);
            update.ID = defect.ID;
            update.OwnersIds = defect.OwnersIds;

            if (issue.HasAssignee()) // Assign Owner
            {
                var member = await TrySyncMemberFromJiraUser(issue.Fields.Assignee);
                if (member != null && !update.OwnersIds.Any(i => i.Equals(member.Oid())))
                    await _v1.UpdateAsset(update, update.CreateOwnersPayload(member.Oid()));
            }
            else if (update.OwnersIds.Any()) // Unassign Owner
            {
                await _v1.UpdateAsset(update, update.CreateOwnersPayload());
            }

            if (!issue.ItMatchesDefect(defect))
            {
                update.Super = v1EpicId;
                _log.TraceFormat("Attempting to update V1 defect {0}", defect.Number);
                await _v1.UpdateAsset(update, update.CreateUpdatePayload());
                _log.DebugFormat("Updated V1 defect {0}", defect.Number);
                defectUpdatedClosed = 1;
            }

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && defect.AssetState != "128")
            {
                await _v1.CloseDefect(defect.ID);
                _log.DebugClosedItem("defect", defect.Number);
                defectUpdatedClosed = 2;
            }

            return defectUpdatedClosed;
        }

        public void CreateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraBugs, List<Defect> allV1Defects)
        {
            _log.Trace("Creating defects started");
            var processedDefects = 0;
            var newDefects = allJiraBugs.Where(bug =>
            {
                if (allV1Defects.Any(x => bug.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Defects.SingleOrDefault(vDefect => !string.IsNullOrWhiteSpace(vDefect.Reference) &&
                                                              vDefect.Reference.Contains(bug.Key)) == null;
            }).ToList();

            if (newDefects.Any())
                _log.DebugFormat("Found {0} defects to check for create", newDefects.Count);

            newDefects.ForEach(bug =>
            {
                CreateDefectFromJira(jiraInfo, bug).Wait();
                processedDefects++;
            });

            if (processedDefects > 0)
                _log.InfoCreated(processedDefects, PluralAsset);
            _log.TraceCreateFinished(PluralAsset);
        }

        public async Task CreateDefectFromJira(V1JiraInfo jiraInfo, Issue jiraBug)
        {
            var defect = jiraBug.ToV1Defect(jiraInfo.V1ProjectId);

            if (jiraBug.HasAssignee())
            {
                var member = await TrySyncMemberFromJiraUser(jiraBug.Fields.Assignee);
                if (member != null)
                    defect.OwnersIds.Add(member.Oid());
            }

            if (!string.IsNullOrEmpty(jiraBug.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraBug.Fields.EpicLink);
                defect.Super = epicId;
            }

            _log.TraceFormat("Attempting to create V1 defect from Jira defect {0}", jiraBug.Key);
            var newDefect = await _v1.CreateDefect(defect);
            _log.DebugFormat("Created {0} from Jira defect {1}", newDefect.Number, jiraBug.Key);

            await _v1.RefreshBasicInfo(newDefect);

            jiraInfo.JiraInstance.UpdateIssue(newDefect.ToIssueWithOnlyNumberAsLabel(jiraBug.Fields.Labels), jiraBug.Key);
            _log.TraceFormat("Updated labels on Jira defect {0}", jiraBug.Key);

            jiraInfo.JiraInstance.AddComment(jiraBug.Key, string.Format(CreatedFromV1Comment, newDefect.Number, newDefect.ScopeName));
            _log.TraceFormat("Added comment to Jira defect {0}", jiraBug.Key);

            jiraInfo.JiraInstance.AddWebLink(jiraBug.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, newDefect.Number),
                        string.Format(V1AssetDetailWebLinkTitle, newDefect.Number));
            _log.TraceFormat("Added web link to V1 defect {0} on Jira bug {1}", newDefect.Number, jiraBug.Key);

            var link = new Uri(new Uri(jiraInfo.JiraInstance.InstanceUrl), string.Format("browse/{0}", jiraBug.Key)).ToString();
            _v1.CreateLink(newDefect, string.Format("Jira {0}", jiraBug.Key), link);
            _log.TraceFormat("Added link in V1 defect {0}", newDefect.Number);
        }

        public void DeleteV1Defects(V1JiraInfo jiraInfo, List<Issue> allJiraBugs, List<Defect> allV1Defects)
        {
            _log.Trace("Deleting defects started");
            var processedDefects = 0;

            var jiraReferencedBugsKeys =
                allV1Defects.Where(v1Defect => !v1Defect.IsInactive && !string.IsNullOrWhiteSpace(v1Defect.Reference))
                    .Select(v1Defect => v1Defect.Reference);

            var jiraDeletedBugsKeys =
                jiraReferencedBugsKeys.Where(jiraDefectKey => !allJiraBugs.Any(js => js.Key.Equals(jiraDefectKey))).ToList();

            if (jiraDeletedBugsKeys.Any())
                _log.DebugFormat("Found {0} defects to delete", jiraDeletedBugsKeys.Count);

            jiraDeletedBugsKeys.ForEach(key =>
            {
                _log.TraceFormat("Attempting to delete V1 defect referencing jira defect {0}", key);
                _v1.DeleteDefectWithJiraReference(jiraInfo.V1ProjectId, key);
                _log.DebugFormat("Deleted V1 defect referencing jira defect {0}", key);
                processedDefects++;
            });

            if (processedDefects > 0)
                _log.InfoDelete(processedDefects, PluralAsset);
            _log.TraceDeleteFinished(PluralAsset);
        }

        private async Task<Member> TrySyncMemberFromJiraUser(User jiraUser)
        {
            Member member = null;
            try
            {
                member = await _v1.SyncMemberFromJiraUser(jiraUser);
            }
            catch (Exception e)
            {
                _log.WarnFormat("Can not get or create VersionOne Member for Jira User '{0}'", jiraUser.name);
                _log.Error(e);
            }

            return member;
        }
    }
}
