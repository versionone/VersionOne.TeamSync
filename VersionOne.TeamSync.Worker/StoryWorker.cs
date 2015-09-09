﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker
{
    public class StoryWorker : IAsyncWorker
    {
        private const string PluralAsset = "stories";
        private const string CreatedFromV1Comment = "Created from VersionOne Work Item {0} in Project {1}";
        private const string V1AssetDetailWebLinkUrl = "{0}assetdetail.v1?Number={1}";
        private const string V1AssetDetailWebLinkTitle = "VersionOne Story ({0})";

        private readonly IV1 _v1;
        private readonly ILog _log;

        public StoryWorker(IV1 v1, ILog log)
        {
            _v1 = v1;
            _log = log;
        }

        public async Task DoWork(V1JiraInfo jiraInfo)
        {
            _log.Trace("Story sync started...");
            var allJiraStories = jiraInfo.JiraInstance.GetStoriesInProject(jiraInfo.JiraKey).issues;
            var allV1Stories = await _v1.GetStoriesWithJiraReference(jiraInfo.V1ProjectId);

            UpdateStories(jiraInfo, allJiraStories, allV1Stories);
            await CreateStories(jiraInfo, allJiraStories, allV1Stories);
            DeleteV1Stories(jiraInfo, allJiraStories, allV1Stories);

            _log.Trace("Story sync stopped...");
        }

        public async void UpdateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Updating stories started");
            var updatedStories = 0;
            var closedStories = 0;
            var existingStories =
                allJiraStories.Where(jStory => { return allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            _log.DebugFormat("Found {0} stories to check for update", existingStories.Count);

            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            existingStories.ForEach(existingJStory =>
            {
                var story = allV1Stories.Single(x => existingJStory.Fields.Labels.Contains(x.Number));

                var returnValue = UpdateStoryFromJiraToV1(jiraInfo, existingJStory, story, assignedEpics);

                switch (returnValue)
                {
                    case 1:
                        updatedStories++;
                        break;
                    case 2:
                        closedStories++;
                        break;
                } 
                
            });

            _log.InfoUpdated(updatedStories, PluralAsset);
            _log.InfoClosed(closedStories, PluralAsset);
            _log.TraceUpdateFinished(PluralAsset);
        }

        public int  UpdateStoryFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Story story, List<Epic> assignedEpics)
        {
            int defectUpdatedClosed = 0;
            _log.TraceFormat("Attempting to update V1 story {0}", story.Number);

            //need to reopen a story first before we can update it
            if (issue.Fields.Status != null && !issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && story.AssetState == "128")
            {
                _v1.ReOpenStory(story.ID);
                _log.DebugFormat("Reopened story V1 {0}", story.Number);
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;
            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;
            var update = issue.ToV1Story(jiraInfo.V1ProjectId);
            update.ID = story.ID;
            update.OwnersIds = story.OwnersIds;

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

            if (!issue.ItMatchesStory(story))
            {
                update.Super = v1EpicId;
                _v1.UpdateAsset(update, update.CreateUpdatePayload());
                _log.DebugFormat("Updated story V1 {0}", story.Number);
                defectUpdatedClosed = 1;
            }

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && story.AssetState != "128")
            {
                _v1.CloseStory(story.ID);
                _log.DebugClosedItem("story", story.Number);
                defectUpdatedClosed = 2;
            }

            return defectUpdatedClosed;

            //var x = issue.Fields.Sprints
        }

        public async Task CreateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Creating stories started");
            var processedStories = 0;
            var newStories = allJiraStories.Where(jStory =>
            {
                if (allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vStory => !string.IsNullOrWhiteSpace(vStory.Reference) &&
                                                              vStory.Reference.Contains(jStory.Key)) == null;
            }).ToList();

            _log.DebugFormat("Found {0} stories to check for create", newStories.Count);

            newStories.ForEach(newJStory =>
            {
                CreateStoryFromJira(jiraInfo, newJStory).Wait();
                processedStories++;
            });

            _log.InfoCreated(processedStories, PluralAsset);
            _log.TraceCreateFinished(PluralAsset);
        }

        public async Task CreateStoryFromJira(V1JiraInfo jiraInfo, Issue jiraStory)
        {
            _log.TraceFormat("Attempting to create story from Jira story {0}", jiraStory.Key);
            var story = jiraStory.ToV1Story(jiraInfo.V1ProjectId);

            if (jiraStory.HasAssignee())
            {
                var member = await TrySyncMemberFromJiraUser(jiraStory.Fields.Assignee);
                if (member != null)
                    story.OwnersIds.Add(member.Oid());
            }

            if (!string.IsNullOrEmpty(jiraStory.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraStory.Fields.EpicLink);
                story.Super = epicId;
            }

            var newStory = await _v1.CreateStory(story);
            _log.DebugFormat("Created {0} from Jira story {1}", newStory.Number, jiraStory.Key);

            await _v1.RefreshBasicInfo(newStory);

            jiraInfo.JiraInstance.UpdateIssue(newStory.ToIssueWithOnlyNumberAsLabel(jiraStory.Fields.Labels), jiraStory.Key);
            _log.TraceFormat("Updated labels on Jira story {0}", jiraStory.Key);

            jiraInfo.JiraInstance.AddComment(jiraStory.Key, string.Format(CreatedFromV1Comment, newStory.Number, newStory.ScopeName));
            _log.TraceFormat("Added comment to Jira story {0}", jiraStory.Key);

            jiraInfo.JiraInstance.AddWebLink(jiraStory.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, newStory.Number),
                        string.Format(V1AssetDetailWebLinkTitle, newStory.Number));
            _log.TraceFormat("Added web link to V1 story {0} on Jira story {1}", newStory.Number, jiraStory.Key);

            var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraStory.Key;
            _v1.CreateLink(newStory, string.Format("Jira {0}", jiraStory.Key), link);
            _log.TraceFormat("Added link in V1 story {0}", newStory.Number);
        }

        public void DeleteV1Stories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Deleting stories started");
            var processedStories = 0;
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Story => !v1Story.IsInactive && !string.IsNullOrWhiteSpace(v1Story.Reference))
                    .Select(v1Story => v1Story.Reference);
            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraStoryKey => !allJiraStories.Any(js => js.Key.Equals(jiraStoryKey))).ToList();

            _log.DebugFormat("Found {0} stories to check for delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                _log.TraceFormat("Attempting to delete V1 story referencing jira story {0}", key);
                _v1.DeleteStoryWithJiraReference(jiraInfo.V1ProjectId, key);
                _log.DebugFormat("Deleted V1 story referencing jira story {0}", key);
                processedStories++;
            });

            _log.InfoDelete(processedStories, PluralAsset);
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
