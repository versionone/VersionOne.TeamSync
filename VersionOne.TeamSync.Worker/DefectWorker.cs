﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VersionOne.TeamSync.Core.Extensions;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.JiraWorker.Extensions;
using VersionOne.TeamSync.VersionOne.Domain;
using VersionOne.TeamSync.VersionOne.Extensions;

namespace VersionOne.TeamSync.JiraWorker
{
    public class DefectWorker : IAsyncWorker
    {
        private const string PluralAsset = "defects";
        private const string CreatedFromV1Comment = "Created from VersionOne Work Item {0} in Project {1}";
        private const string V1AssetDetailWebLinkUrl = "{0}assetdetail.v1?Number={1}";
        private const string V1AssetDetailWebLinkTitle = "VersionOne Defect ({0})";

        private readonly IV1 _v1;
        private readonly IV1Log _log;

        public DefectWorker(IV1 v1, IV1Log log)
        {
            _v1 = v1;
            _log = log;
        }

        public async Task DoFirstRun(IJira jiraInstance)
        {
            _log.Trace("Defect sync started...");
            var allJiraBugs = jiraInstance.GetAllBugsInProjectSince(jiraInstance.JiraProject, jiraInstance.RunFromThisDateOn).issues;
            var allV1Defects = await _v1.GetDefectsWithJiraReferenceCreatedSince(jiraInstance.V1Project, jiraInstance.RunFromThisDateOn);

            UpdateDefects(jiraInstance, allJiraBugs, allV1Defects);
            CreateDefects(jiraInstance, allJiraBugs, allV1Defects);
            DeleteV1Defects(jiraInstance, allJiraBugs, allV1Defects);
            _log.Trace("Defect sync stopped...");
        }

        public async Task DoWork(IJira jiraInstance)
        {
            _log.Trace("Defect sync started...");
            var allJiraBugs = jiraInstance.GetBugsInProject(jiraInstance.JiraProject).issues;
            var allV1Defects = await _v1.GetDefectsWithJiraReference(jiraInstance.V1Project);

            UpdateDefects(jiraInstance, allJiraBugs, allV1Defects);
            CreateDefects(jiraInstance, allJiraBugs, allV1Defects);
            DeleteV1Defects(jiraInstance, allJiraBugs, allV1Defects);
            _log.Trace("Defect sync stopped...");
        }

        public async void UpdateDefects(IJira jiraInstance, List<Issue> allJiraBugs, List<Defect> allV1Defects)
        {
            _log.Trace("Updating defects started");
            var data = new Dictionary<string, int>();
            data["reopened"] = 0;
            data["updated"] = 0;
            data["closed"] = 0;

            var existingBugs =
                allJiraBugs.Where(bug => allV1Defects.Any(defect => bug.Fields.Labels.Contains(defect.Number))).ToList();

            if (existingBugs.Any())
                _log.DebugFormat("Found {0} defects to check for update", existingBugs.Count);

            var assignedEpics = await _v1.GetEpicsWithReference(jiraInstance.V1Project, jiraInstance.EpicCategory);

            existingBugs.ForEach(existingJDefect =>
            {
                var defectToUpdate = allV1Defects.Single(defect => existingJDefect.Fields.Labels.Contains(defect.Number));
                UpdateDefectFromJiraToV1(jiraInstance, existingJDefect, defectToUpdate, assignedEpics, data).Wait();
            });

            if (data["updated"] > 0)
                _log.InfoUpdated(data["updated"], PluralAsset);

            if (data["closed"] > 0)
                _log.InfoClosed(data["closed"], PluralAsset);

            if (data["reopened"] > 0)
                _log.InfoUpdated(data["reopened"], PluralAsset);

            _log.TraceUpdateFinished(PluralAsset);
        }

        public async Task<Dictionary<string, int>> UpdateDefectFromJiraToV1(IJira jiraInstance, Issue issue, Defect defect, List<Epic> assignedEpics, Dictionary<string, int> data)
        {
            string v1StatusId = null;
            if (issue.Fields.Status != null)
            {
                v1StatusId = await _v1.GetStatusIdFromName(JiraSettings.GetInstance().GetV1StatusFromMapping(jiraInstance.InstanceUrl, jiraInstance.JiraProject, issue.Fields.Status.Name));

                //need to reopen a defect first before we can update it
                if (!issue.Fields.Status.Name.Is(jiraInstance.DoneWords) && defect.AssetState == "128")
                {
                    await _v1.ReOpenDefect(defect.ID);
                    _log.DebugFormat("Reopened V1 defect {0}", defect.Number);
                    data["reopened"] += 1;
                }
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;
            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;

            var update = issue.ToV1Defect(jiraInstance.V1Project, JiraSettings.GetInstance().GetV1PriorityIdFromMapping(jiraInstance.InstanceUrl, issue.Fields.Priority.Name), v1StatusId);
            update.ID = defect.ID;
            update.OwnersIds = defect.OwnersIds;

            if (issue.HasAssignee()) // Assign Owner
            {
                var member = await TrySyncMemberFromJiraUser(issue.Fields.Assignee);
                if (member != null && !update.OwnersIds.Any(i => i.Equals(member.Oid())))
                    await _v1.UpdateAsset(update, update.CreateOwnersPayload(member.Oid()));
            }
            else if (update.OwnersIds.Any()) // Unassign Owner
                await _v1.UpdateAsset(update, update.CreateOwnersPayload());

            if (currentAssignedEpic != null && currentAssignedEpic.IsClosed())
                _log.Error("Cannot assign a defect to a closed Epic.  The defect will be still be updated, but should be reassigned to an open Epic");

            if (!issue.ItMatchesDefect(defect) || update.Priority != defect.Priority || update.Status != defect.Status)
            {
                if (currentAssignedEpic != null && !currentAssignedEpic.IsClosed())
                    update.Super = v1EpicId;

                _log.TraceFormat("Attempting to update V1 defect {0}", defect.Number);

                await _v1.UpdateAsset(update, update.CreateUpdatePayload());
                _log.DebugFormat("Updated V1 defect {0}", defect.Number);
                data["updated"] += 1;
            }

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(jiraInstance.DoneWords) && defect.AssetState != "128")
            {
                await _v1.CloseDefect(defect.ID);
                _log.DebugClosedItem("defect", defect.Number);
                data["closed"] += 1;
            }

            return data;
        }

        public void CreateDefects(IJira jiraInstance, List<Issue> allJiraBugs, List<Defect> allV1Defects)
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
                if (CreateDefectFromJira(jiraInstance, bug).Result)
                    processedDefects++;
            });

            if (processedDefects > 0)
                _log.InfoCreated(processedDefects, PluralAsset);

            _log.TraceCreateFinished(PluralAsset);
        }

        public async Task<bool> CreateDefectFromJira(IJira jiraInstance, Issue jiraBug)
        {
            var v1StatusId = await _v1.GetStatusIdFromName(JiraSettings.GetInstance().GetV1StatusFromMapping(jiraInstance.InstanceUrl, jiraInstance.JiraProject, jiraBug.Fields.Status.Name));
            var defect = jiraBug.ToV1Defect(jiraInstance.V1Project, JiraSettings.GetInstance().GetV1PriorityIdFromMapping(jiraInstance.InstanceUrl, jiraBug.Fields.Priority.Name), v1StatusId);

            if (!string.IsNullOrEmpty(jiraBug.Fields.EpicLink))
            {
                var epic = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraBug.Fields.EpicLink);
                if (epic != null)
                {
                    if (epic.IsClosed)
                    {
                        _log.Error("Unable to assign epic " + jiraBug.Fields.EpicLink + " -- Epic may be closed");
                        return false;
                    }
                    defect.Super = epic.Token;
                }
            }

            if (jiraBug.HasAssignee())
            {
                var member = await TrySyncMemberFromJiraUser(jiraBug.Fields.Assignee);
                if (member != null)
                    defect.OwnersIds.Add(member.Oid());
            }

            _log.TraceFormat("Attempting to create V1 defect from Jira defect {0}", jiraBug.Key);

            var newDefect = await _v1.CreateDefect(defect);
            _log.DebugFormat("Created {0} from Jira defect {1}", newDefect.Number, jiraBug.Key);

            await _v1.RefreshBasicInfo(newDefect);

            // If story is closed we have to reopen it
            var status = jiraInstance.DoneWords.FirstOrDefault(dw => dw.Equals(jiraBug.Fields.Status.Name));
            if (status != null)
            {
                string transitionIdToRun = jiraInstance.GetIssueTransitionId(jiraBug.Key, Jira.ReopenedStatus);
                if (transitionIdToRun != null)
                    jiraInstance.RunTransitionOnIssue(transitionIdToRun, jiraBug.Key);
            }

            jiraInstance.UpdateIssue(newDefect.ToIssueWithOnlyNumberAsLabel(jiraBug.Fields.Labels), jiraBug.Key);
            _log.TraceFormat("Updated labels on Jira defect {0}", jiraBug.Key);

            jiraInstance.AddComment(jiraBug.Key, string.Format(CreatedFromV1Comment, newDefect.Number, newDefect.ScopeName));
            _log.TraceFormat("Added comment to Jira defect {0}", jiraBug.Key);

            jiraInstance.AddWebLink(jiraBug.Key,
                       string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, newDefect.Number),
                       string.Format(V1AssetDetailWebLinkTitle, newDefect.Number));
            _log.TraceFormat("Added web link to V1 defect {0} on Jira bug {1}", newDefect.Number, jiraBug.Key);

            // If story is reopened we have to close it
            if (status != null)
            {
                string transitionIdToRun = jiraInstance.GetIssueTransitionId(jiraBug.Key, status);
                if (transitionIdToRun != null)
                    jiraInstance.RunTransitionOnIssue(transitionIdToRun, jiraBug.Key);
            }

            var link = new Uri(new Uri(jiraInstance.InstanceUrl), string.Format("browse/{0}", jiraBug.Key)).ToString();
            _v1.CreateLink(newDefect, string.Format("Jira {0}", jiraBug.Key), link);
            _log.TraceFormat("Added link in V1 defect {0}", newDefect.Number);

            return true;
        }

        public void DeleteV1Defects(IJira jiraInstance, List<Issue> allJiraBugs, List<Defect> allV1Defects)
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
                _v1.DeleteDefectWithJiraReference(jiraInstance.V1Project, key);
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
                member = await _v1.GetMember(jiraUser.name);
                if (member != null && !jiraUser.ItMatchesMember(member))
                {
                    member.Name = jiraUser.displayName;
                    member.Nickname = jiraUser.name;
                    member.Email = jiraUser.emailAddress;
                    await _v1.UpdateMember(member);
                }
                else if (member == null)
                {
                    member = await _v1.CreateMember(jiraUser.ToV1Member());
                }
            }
            catch (Exception e)
            {
                _log.WarnFormat("Cannot get or create VersionOne Member for Jira User '{0}'", jiraUser.name);
                _log.Error(e);
            }

            return member;
        }
    }
}
