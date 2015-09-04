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
            var allJiraDefects = jiraInfo.JiraInstance.GetDefectsInProject(jiraInfo.JiraKey).issues;
            var allV1Defects = await _v1.GetDefectsWithJiraReference(jiraInfo.V1ProjectId);

            UpdateDefects(jiraInfo, allJiraDefects, allV1Defects);
            CreateDefects(jiraInfo, allJiraDefects, allV1Defects);
            DeleteV1Defects(jiraInfo, allJiraDefects, allV1Defects);

            _log.Trace("Defect sync stopped...");
        }

        public async void UpdateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraDefects, List<Defect> allV1Defects)
        {
            _log.Trace("Updating defects started");
            var processedDefects = 0;
            var existingDefects =
                allJiraDefects.Where(jDefect => { return allV1Defects.Any(x => jDefect.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            _log.DebugFormat("Found {0} defects to check for update", existingDefects.Count);
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            existingDefects.ForEach(existingJDefect =>
            {
                var defect = allV1Defects.Single(x => existingJDefect.Fields.Labels.Contains(x.Number));

                UpdateDefectFromJiraToV1(jiraInfo, existingJDefect, defect, assignedEpics).Wait();
                processedDefects++;
            });

            _log.InfoUpdated(processedDefects, PluralAsset);
            _log.TraceUpdateFinished(PluralAsset);
        }

        public async Task UpdateDefectFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Defect defect, List<Epic> assignedEpics)
        {
            //need to reopen a Defect first before we can update it
            if (issue.Fields.Status != null && !issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && defect.AssetState == "128")
            {
                await _v1.ReOpenDefect(defect.ID);
                _log.TraceFormat("Reopened V1 defect {0}", defect.Number);
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;

            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;

            var update = issue.ToV1Defect(jiraInfo.V1ProjectId);
            update.ID = defect.ID;

            if (!issue.ItMatchesDefect(defect))
            {
                update.Super = v1EpicId;
                _log.TraceFormat("Attempting to update V1 defect {0}", defect.Number);
                await _v1.UpdateAsset(update, update.CreateUpdatePayload()).ContinueWith(task =>
                {
                    _log.DebugFormat("Updated V1 defect {0}", defect.Number);
                });
            }

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && defect.AssetState != "128")
            {
                await _v1.CloseDefect(defect.ID);
                _log.DebugClosedItem("defect", defect.Number);
            }
        }

        public void CreateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            _log.Trace("Creating defects started");
            var processedDefects = 0;
            var newStories = allJiraStories.Where(jDefect =>
            {
                if (allV1Stories.Any(x => jDefect.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vDefect => !string.IsNullOrWhiteSpace(vDefect.Reference) &&
                                                              vDefect.Reference.Contains(jDefect.Key)) == null;
            }).ToList();

            _log.DebugFormat("Found {0} defects to check for create", newStories.Count);

            newStories.ForEach(async newJDefect =>
            {
                await CreateDefectFromJira(jiraInfo, newJDefect);
                processedDefects++;
            });

            _log.InfoCreated(processedDefects, PluralAsset);
            _log.TraceCreateFinished(PluralAsset);
        }

        public async Task CreateDefectFromJira(V1JiraInfo jiraInfo, Issue jiraDefect)
        {
            var defect = jiraDefect.ToV1Defect(jiraInfo.V1ProjectId);

            if (!string.IsNullOrEmpty(jiraDefect.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraDefect.Fields.EpicLink);
                defect.Super = epicId;
            }

            _log.TraceFormat("Attempting to create V1 defect from Jira defect {0}", jiraDefect.Key);
            var newDefect = await _v1.CreateDefect(defect);
            _log.DebugFormat("Created {0} from Jira defect {1}", newDefect.Number, jiraDefect.Key);

            await _v1.RefreshBasicInfo(newDefect);

            jiraInfo.JiraInstance.UpdateIssue(newDefect.ToIssueWithOnlyNumberAsLabel(jiraDefect.Fields.Labels), jiraDefect.Key);
            _log.TraceFormat("Updated labels on Jira defect {0}", jiraDefect.Key);

            jiraInfo.JiraInstance.AddComment(jiraDefect.Key, string.Format(CreatedFromV1Comment, newDefect.Number, newDefect.ScopeName));
            _log.TraceFormat("Added comment to Jira defect {0}", jiraDefect.Key);

            jiraInfo.JiraInstance.AddWebLink(jiraDefect.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, newDefect.Number),
                        string.Format(V1AssetDetailWebLinkTitle, newDefect.Number));
            _log.TraceFormat("Added web link to V1 story {0} on Jira story {1}", newDefect.Number, jiraDefect.Key);

            var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraDefect.Key;
            _v1.CreateLink(newDefect, string.Format("Jira {0}", jiraDefect.Key), link);
            _log.TraceFormat("Added link in V1 defect {0}", newDefect.Number);
        }

        public void DeleteV1Defects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            _log.Trace("Deleting defects started");
            var processedDefects = 0;
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Defect => !v1Defect.IsInactive && !string.IsNullOrWhiteSpace(v1Defect.Reference))
                    .Select(v1Defect => v1Defect.Reference);

            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraDefectKey => !allJiraStories.Any(js => js.Key.Equals(jiraDefectKey))).ToList();

            _log.DebugFormat("Found {0} defects to delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                _log.TraceFormat("Attempting to delete V1 defect referencing jira defect {0}", key);
                _v1.DeleteDefectWithJiraReference(jiraInfo.V1ProjectId, key);
                _log.DebugFormat("Deleted V1 defect referencing jira defect {0}", key);
                processedDefects++;
            });

            _log.InfoDelete(processedDefects, PluralAsset);
            _log.TraceDeleteFinished(PluralAsset);
        }

    }
}
