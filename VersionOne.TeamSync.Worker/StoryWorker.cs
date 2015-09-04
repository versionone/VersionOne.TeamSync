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
    public class StoryWorker : IAsyncWorker
    {
        private readonly IV1 _v1;
	private string _pluralAsset = "stories";
	public static ILog Log { get; private set; }
        private const string CreatedFromV1Comment = "Created from VersionOne Work Item {0} in Project {1}";
        private const string V1AssetDetailWebLinkUrl = "{0}assetdetail.v1?Number={1}";
        private const string V1AssetDetailWebLinkTitle = "VersionOne Story ({0})";

        public StoryWorker(IV1 v1, ILog log)
        {
            _v1 = v1;
            Log = log;
        }

        public async Task DoWork(V1JiraInfo jiraInfo)
        {
            Log.Trace("Story sync started...");
            var allJiraStories = jiraInfo.JiraInstance.GetStoriesInProject(jiraInfo.JiraKey).issues;
            var allV1Stories = await _v1.GetStoriesWithJiraReference(jiraInfo.V1ProjectId);

            UpdateStories(jiraInfo, allJiraStories, allV1Stories);
            CreateStories(jiraInfo, allJiraStories, allV1Stories);
            DeleteV1Stories(jiraInfo, allJiraStories, allV1Stories);

            Log.Trace("Story sync stopped...");
        }

        public async void UpdateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            Log.Trace("Updating stories started");
            var processedStories = 0;
            var existingStories =
                allJiraStories.Where(jStory => { return allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            Log.DebugFormat("Found {0} stories to check for update", existingStories.Count);

            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            existingStories.ForEach(existingJStory =>
            {
                var story = allV1Stories.Single(x => existingJStory.Fields.Labels.Contains(x.Number));

                UpdateStoryFromJiraToV1(jiraInfo, existingJStory, story, assignedEpics).Wait();
                processedStories++;
            });

	    Log.InfoUpdated(processedStories, _pluralAsset);
	    Log.TraceUpdateFinished(_pluralAsset);
        }

        public async Task UpdateStoryFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Story story, List<Epic> assignedEpics)
        {
            Log.TraceFormat("Attempting to update V1 story {0}", story.Number);

            //need to reopen a story first before we can update it
            if (issue.Fields.Status != null && !issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && story.AssetState == "128")
            {
                await _v1.ReOpenStory(story.ID);
                Log.DebugFormat("Reopened story V1 {0}", story.Number);
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;
            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;
            var update = issue.ToV1Story(jiraInfo.V1ProjectId);
            update.ID = story.ID;
            update.OwnersIds = story.OwnersIds;

            if (issue.HasAssignee())
            {
                string ownerOid;
                var assigneeName = issue.Fields.Assignee.Name;
                var storyOwner = await _v1.GetMember(assigneeName);
                if (storyOwner != null)
                {
                    if (!issue.Fields.Assignee.ItMatchesMember(storyOwner))
                    {
                        storyOwner.Name = issue.Fields.Assignee.DisplayName;
                        storyOwner.Nickname = assigneeName;
                        storyOwner.Email = issue.Fields.Assignee.EmailAddress;
                        await _v1.UpdateAsset(storyOwner, storyOwner.CreateUpdatePayload());
                    }
                    ownerOid = string.Format("{0}:{1}", storyOwner.AssetType, storyOwner.ID);
                }
                else
                {
                    var member = await GetV1MemberFromAssignee(issue);
                    ownerOid = string.Format("{0}:{1}", member.AssetType, member.ID);
                }
                if (!update.OwnersIds.Any(i => i.Equals(ownerOid)))
                    await _v1.UpdateAsset(update, update.CreateOwnersPayload(ownerOid));
            }
            else if (update.OwnersIds.Any())
            {
                await _v1.UpdateAsset(update,update.CreateOwnersPayload());
            }
            
            if (!issue.ItMatchesStory(story))
            {
                update.Super = v1EpicId;
                await _v1.UpdateAsset(update, update.CreateUpdatePayload());
                Log.DebugFormat("Updated story V1 {0}", story.Number);
            }

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(jiraInfo.DoneWords) && story.AssetState != "128")
            {
                await _v1.CloseStory(story.ID);
                Log.DebugClosedItem("story", story.Number);
            }
        }

        public void CreateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            Log.Trace("Creating stories started");
            var processedStories = 0;
            var newStories = allJiraStories.Where(jStory =>
            {
                if (allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vStory => !string.IsNullOrWhiteSpace(vStory.Reference) &&
                                                              vStory.Reference.Contains(jStory.Key)) == null;
            }).ToList();

            Log.DebugFormat("Found {0} stories to check for create", newStories.Count);

            newStories.ForEach(async newJStory =>
            {
                await CreateStoryFromJira(jiraInfo, newJStory);
                processedStories++;
            });

	    Log.InfoCreated(processedStories, _pluralAsset);
	    Log.TraceCreateFinished(_pluralAsset);
	}

        public async Task CreateStoryFromJira(V1JiraInfo jiraInfo, Issue jiraStory)
        {
            Log.TraceFormat("Attempting to create story from Jira story {0}", jiraStory.Key);
            var story = jiraStory.ToV1Story(jiraInfo.V1ProjectId);

            if (jiraStory.HasAssignee())
            {
                var member = await GetV1MemberFromAssignee(jiraStory);
                story.OwnersIds.Add(string.Format("{0}:{1}", member.AssetType, member.ID));
            }

            if (!string.IsNullOrEmpty(jiraStory.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraStory.Fields.EpicLink);
                story.Super = epicId;
            }

            var newStory = await _v1.CreateStory(story);
            Log.DebugFormat("Created {0} from Jira story {1}", newStory.Number, jiraStory.Key);

            await _v1.RefreshBasicInfo(newStory);

            jiraInfo.JiraInstance.UpdateIssue(newStory.ToIssueWithOnlyNumberAsLabel(jiraStory.Fields.Labels), jiraStory.Key);
            Log.TraceFormat("Updated labels on Jira story {0}", jiraStory.Key);

            jiraInfo.JiraInstance.AddComment(jiraStory.Key, string.Format(CreatedFromV1Comment, newStory.Number, newStory.ScopeName));
            Log.TraceFormat("Added comment to Jira story {0}", jiraStory.Key);

            jiraInfo.JiraInstance.AddWebLink(jiraStory.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, newStory.Number),
                        string.Format(V1AssetDetailWebLinkTitle, newStory.Number));
            Log.TraceFormat("Added web link to V1 story {0} on Jira story {1}", newStory.Number, jiraStory.Key);

            var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraStory.Key;
            _v1.CreateLink(newStory, string.Format("Jira {0}", jiraStory.Key), link);
            Log.TraceFormat("Added link in V1 story {0}", newStory.Number);
        }

        private async Task<Member> GetV1MemberFromAssignee(Issue jiraStory)
        {
            return await _v1.GetMember(jiraStory.Fields.Assignee.Name) ??
                         await _v1.CreateMember(jiraStory.Fields.Assignee.ToV1Member());
        }

        public void DeleteV1Stories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            Log.Trace("Deleting stories started");
            var processedStories = 0;
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Story => !v1Story.IsInactive && !string.IsNullOrWhiteSpace(v1Story.Reference))
                    .Select(v1Story => v1Story.Reference);
            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraStoryKey => !allJiraStories.Any(js => js.Key.Equals(jiraStoryKey))).ToList();

            Log.DebugFormat("Found {0} stories to check for delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                Log.TraceFormat("Attempting to delete V1 story referencing jira story {0}", key);
                _v1.DeleteStoryWithJiraReference(jiraInfo.V1ProjectId, key);
                Log.DebugFormat("Deleted V1 story referencing jira story {0}", key);
                processedStories++;
            });

            Log.InfoDelete(processedStories, _pluralAsset);
	    Log.TraceDeleteFinished(_pluralAsset);
        }

    }
}
