﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly IV1 _v1;
        private string _pluralAsset = "defects";
        public static ILog Log { get; private set; }
        private const string CreatedFromV1Comment = "Created from VersionOne Work Item {0} in Project {1}";
        private const string V1AssetDetailWebLinkUrl = "{0}assetdetail.v1?Number={1}";
        private const string V1AssetDetailWebLinkTitle = "VersionOne Defect ({0})";

        public DefectWorker(IV1 v1, ILog log)
        {
            _v1 = v1;
            Log = log;
        }


        public async Task DoWork(V1JiraInfo jiraInfo)
        {
            Log.Trace("Defect sync started...");
            var allJiraDefects = jiraInfo.JiraInstance.GetDefectsInProject(jiraInfo.JiraKey).issues;
            var allV1Defects = await _v1.GetDefectsWithJiraReference(jiraInfo.V1ProjectId);

            UpdateDefects(jiraInfo, allJiraDefects, allV1Defects);
            CreateDefects(jiraInfo, allJiraDefects, allV1Defects);
            DeleteV1Defects(jiraInfo, allJiraDefects, allV1Defects);

            Log.Trace("Defect sync stopped...");
        }

        public async void UpdateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraDefects, List<Defect> allV1Defects)
        {
            Log.Trace("Updating defects started");
            var processedDefects = 0;
            var existingDefects =
                allJiraDefects.Where(jDefect => { return allV1Defects.Any(x => jDefect.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            Log.DebugFormat("Found {0} defects to check for update", existingDefects.Count);
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            existingDefects.ForEach(existingJDefect =>
            {
                var defect = allV1Defects.Single(x => existingJDefect.Fields.Labels.Contains(x.Number));

                UpdateDefectFromJiraToV1(jiraInfo, existingJDefect, defect, assignedEpics).Wait();
                processedDefects++;
            });

            Log.InfoUpdated(processedDefects, _pluralAsset);
            Log.TraceUpdateFinished(_pluralAsset);
        }

        public async Task UpdateDefectFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Defect defect, List<Epic> assignedEpics)
        {
            //need to reopen a Defect first before we can update it
            if (issue.Fields.Status != null && !issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && defect.AssetState == "128")
            {
                await _v1.ReOpenDefect(defect.ID);
                Log.TraceFormat("Reopened V1 defect {0}", defect.Number);
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;
            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;
            var update = issue.ToV1Defect(jiraInfo.V1ProjectId);
            update.ID = defect.ID;
            update.OwnersIds = defect.OwnersIds;

            if (issue.HasAssignee())
            {
                string ownerOid;
                var assigneeName = issue.Fields.Assignee.name;
                var owner = await _v1.GetMember(assigneeName);
                if (owner != null)
                {
                    if (!issue.Fields.Assignee.ItMatchesMember(owner))
                    {
                        owner.Name = issue.Fields.Assignee.displayName;
                        owner.Nickname = assigneeName;
                        owner.Email = issue.Fields.Assignee.emailAddress;
                        await _v1.UpdateAsset(owner, owner.CreateUpdatePayload());
                    }
                    ownerOid = owner.Oid();
                }
                else
                {
                    var member = await TryGetMemberFromJiraUser(issue.Fields.Assignee);
                    ownerOid = member != null ? member.Oid() : null;
                }
                if (!update.OwnersIds.Any(i => i.Equals(ownerOid)))
                    await _v1.UpdateAsset(update, update.CreateOwnersPayload(ownerOid));
            }
            else if (update.OwnersIds.Any())
            {
                await _v1.UpdateAsset(update, update.CreateOwnersPayload());
            }

            if (!issue.ItMatchesDefect(defect))
            {
                update.Super = v1EpicId;
                Log.TraceFormat("Attempting to update V1 defect {0}", defect.Number);
                await _v1.UpdateAsset(update, update.CreateUpdatePayload()).ContinueWith(task =>
                {
                    Log.DebugFormat("Updated V1 defect {0}", defect.Number);
                });
            }

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && defect.AssetState != "128")
            {
                await _v1.CloseDefect(defect.ID);
                Log.DebugClosedItem("defect", defect.Number);
            }
        }

        public void CreateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            Log.Trace("Creating defects started");
            var processedDefects = 0;
            var newStories = allJiraStories.Where(jDefect =>
            {
                if (allV1Stories.Any(x => jDefect.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vDefect => !string.IsNullOrWhiteSpace(vDefect.Reference) &&
                                                              vDefect.Reference.Contains(jDefect.Key)) == null;
            }).ToList();

            Log.DebugFormat("Found {0} defects to check for create", newStories.Count);

            newStories.ForEach(async newJDefect =>
            {
                await CreateDefectFromJira(jiraInfo, newJDefect);
                processedDefects++;
            });

            Log.InfoCreated(processedDefects, _pluralAsset);
            Log.TraceCreateFinished(_pluralAsset);
        }

        public async Task CreateDefectFromJira(V1JiraInfo jiraInfo, Issue jiraDefect)
        {
            var defect = jiraDefect.ToV1Defect(jiraInfo.V1ProjectId);

            if (jiraDefect.HasAssignee())
            {
                var member = await TryGetMemberFromJiraUser(jiraDefect.Fields.Assignee);
                if (member != null)
                    defect.OwnersIds.Add(member.Oid()); 
            }

            if (!string.IsNullOrEmpty(jiraDefect.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraDefect.Fields.EpicLink);
                defect.Super = epicId;
            }

            Log.TraceFormat("Attempting to create V1 defect from Jira defect {0}", jiraDefect.Key);
            var newDefect = await _v1.CreateDefect(defect);
            Log.DebugFormat("Created {0} from Jira defect {1}", newDefect.Number, jiraDefect.Key);

            await _v1.RefreshBasicInfo(newDefect);

            jiraInfo.JiraInstance.UpdateIssue(newDefect.ToIssueWithOnlyNumberAsLabel(jiraDefect.Fields.Labels), jiraDefect.Key);
            Log.TraceFormat("Updated labels on Jira defect {0}", jiraDefect.Key);

            jiraInfo.JiraInstance.AddComment(jiraDefect.Key, string.Format(CreatedFromV1Comment, newDefect.Number, newDefect.ScopeName));
            Log.TraceFormat("Added comment to Jira defect {0}", jiraDefect.Key);

            jiraInfo.JiraInstance.AddWebLink(jiraDefect.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, newDefect.Number),
                        string.Format(V1AssetDetailWebLinkTitle, newDefect.Number));
            Log.TraceFormat("Added web link to V1 story {0} on Jira story {1}", newDefect.Number, jiraDefect.Key);

            var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraDefect.Key;
            _v1.CreateLink(newDefect, string.Format("Jira {0}", jiraDefect.Key), link);
            Log.TraceFormat("Added link in V1 defect {0}", newDefect.Number);
        }

        public void DeleteV1Defects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            Log.Trace("Deleting defects started");
            var processedDefects = 0;
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Defect => !v1Defect.IsInactive && !string.IsNullOrWhiteSpace(v1Defect.Reference))
                    .Select(v1Defect => v1Defect.Reference);

            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraDefectKey => !allJiraStories.Any(js => js.Key.Equals(jiraDefectKey))).ToList();

            Log.DebugFormat("Found {0} defects to delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                Log.TraceFormat("Attempting to delete V1 defect referencing jira defect {0}", key);
                _v1.DeleteDefectWithJiraReference(jiraInfo.V1ProjectId, key);
                Log.DebugFormat("Deleted V1 defect referencing jira defect {0}", key);
                processedDefects++;
            });

            Log.InfoDelete(processedDefects, _pluralAsset);
            Log.TraceDeleteFinished(_pluralAsset);
        }

        private async Task<Member> TryGetMemberFromJiraUser(User jiraUser)
        {
            Member member = null;
            try
            {
                member = await _v1.GetMember(jiraUser.name) ?? await _v1.CreateMember(jiraUser.ToV1Member());
            }
            catch (Exception e)
            {
                Log.WarnFormat("Can not get or create Version One Member for Jira User '{0}'", jiraUser.name);
                Log.Error(e);
            }
                
            return member;
        }
    }
}
