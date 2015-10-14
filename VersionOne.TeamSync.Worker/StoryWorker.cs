using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.JiraConnector.Config;
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

        public bool FirstRunCompleted { get; private set; }

        public StoryWorker(IV1 v1, ILog log)
        {
            _v1 = v1;
            _log = log;
        }

        public async Task DoFirstRun(IJira jiraInstance)
        {
            _log.Trace("Story First Run started...");
            var allJiraStories = jiraInstance.GetAllStoriesInProjectSince(jiraInstance.JiraProject, jiraInstance.RunFromThisDateOn).issues;
            var allV1Stories = await _v1.GetStoriesWithJiraReferenceCreatedSince(jiraInstance.V1Project, jiraInstance.RunFromThisDateOn);

            UpdateStories(jiraInstance, allJiraStories, allV1Stories);
            CreateStories(jiraInstance, allJiraStories, allV1Stories);
            DeleteV1Stories(jiraInstance, allJiraStories, allV1Stories);
            _log.Trace("Story First Run stopped...");
        }

        public async Task DoWork(IJira jiraInstance)
        {

            _log.Trace("Story sync started...");
            var allJiraStories = jiraInstance.GetStoriesInProject(jiraInstance.JiraProject).issues;
            var allV1Stories = await _v1.GetStoriesWithJiraReference(jiraInstance.V1Project);

            UpdateStories(jiraInstance, allJiraStories, allV1Stories);
            CreateStories(jiraInstance, allJiraStories, allV1Stories);
            DeleteV1Stories(jiraInstance, allJiraStories, allV1Stories);
            _log.Trace("Story sync stopped...");
        }

        public void UpdateStories(IJira jiraInstance, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Updating stories started");
            var data = new Dictionary<string, int>();
            data["reopened"] = 0;
            data["updated"] = 0;
            data["closed"] = 0;

            var existingStories =
                allJiraStories.Where(
                    jiraStory => allV1Stories.Any(v1Story => jiraStory.Fields.Labels.Contains(v1Story.Number))).ToList();

            if (existingStories.Any())
                _log.DebugFormat("Found {0} stories to check for update", existingStories.Count);

            var assignedEpics = _v1.GetEpicsWithReference(jiraInstance.V1Project, jiraInstance.EpicCategory).Result;

            existingStories.ForEach(existingJStory =>
            {
                var storyToUpdate = allV1Stories.Single(story => existingJStory.Fields.Labels.Contains(story.Number));
                UpdateStoryFromJiraToV1(jiraInstance, existingJStory, storyToUpdate, assignedEpics, data).Wait();
            });

            if (data["updated"] > 0)
                _log.InfoUpdated(data["updated"], PluralAsset);

            if (data["closed"] > 0)
                _log.InfoClosed(data["closed"], PluralAsset);

            if (data["reopened"] > 0)
                _log.InfoUpdated(data["reopened"], PluralAsset);

            _log.TraceUpdateFinished(PluralAsset);
        }

        public async Task<Dictionary<string, int>> UpdateStoryFromJiraToV1(IJira jiraInstance, Issue issue, Story story, List<Epic> assignedEpics, Dictionary<string, int> data)
        {
            var v1StatusId = string.Empty;
            if (issue.Fields.Status != null)
            {
                v1StatusId = await _v1.GetStatusIdFromName(JiraSettings.GetInstance().GetV1StatusFromMapping(jiraInstance.InstanceUrl, jiraInstance.JiraProject, issue.Fields.Status.Name));

                //need to reopen a story first before we can update it
                if (!issue.Fields.Status.Name.Is(jiraInstance.DoneWords) && story.AssetState == "128")
                {
                    await _v1.ReOpenStory(story.ID);
                    _log.DebugFormat("Reopened story V1 {0}", story.Number);
                    data["reopened"] += 1;
                }
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;
            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;

            var update = issue.ToV1Story(jiraInstance.V1Project, JiraSettings.GetInstance().GetV1PriorityIdFromMapping(jiraInstance.InstanceUrl, issue.Fields.Priority.Name), v1StatusId);
            update.ID = story.ID;
            update.OwnersIds = story.OwnersIds;

            if (issue.HasAssignee()) // Assign Owner
            {
                var member = await TrySyncMemberFromJiraUser(issue.Fields.Assignee);
                if (member != null && !update.OwnersIds.Any(i => i.Equals(member.Oid())))
                    await _v1.UpdateAsset(update, update.CreateOwnersPayload(member.Oid()));
            }
            else if (update.OwnersIds.Any()) // Unassign Owner
                await _v1.UpdateAsset(update, update.CreateOwnersPayload());

            if (currentAssignedEpic != null && currentAssignedEpic.IsClosed())
                _log.Error("Cannot assign a story to a closed Epic.  Story will be still be updated, but reassign to an open Epic");

            if (!issue.ItMatchesStory(story) || update.Priority != story.Priority || update.Status != story.Status)
            {
                if (currentAssignedEpic != null && !currentAssignedEpic.IsClosed())
                    update.Super = v1EpicId;

                _log.TraceFormat("Attempting to update V1 story {0}", story.Number);

                await _v1.UpdateAsset(update, update.CreateUpdatePayload());
                _log.DebugFormat("Updated story V1 {0}", story.Number);
                data["updated"] += 1;
            }

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(jiraInstance.DoneWords) && story.AssetState != "128")
            {
                await _v1.CloseStory(story.ID);
                _log.DebugClosedItem("story", story.Number);
                data["closed"] += 1;
            }

            return data;
        }

        public void CreateStories(IJira jiraInstance, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Creating stories started");
            var processedStories = 0;

            var newStories = allJiraStories.Where(story =>
            {
                if (allV1Stories.Any(x => story.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vStory => !string.IsNullOrWhiteSpace(vStory.Reference) &&
                    vStory.Reference.Contains(story.Key)) == null;
            }).ToList();

            if (newStories.Any())
                _log.DebugFormat("Found {0} stories to check for create", newStories.Count);

            newStories.ForEach(story =>
            {
                if (CreateStoryFromJira(jiraInstance, story).Result)
                    processedStories++;
            });

            if (processedStories > 0)
                _log.InfoCreated(processedStories, PluralAsset);

            _log.TraceCreateFinished(PluralAsset);
        }

        public async Task<bool> CreateStoryFromJira(IJira jiraInstance, Issue jiraStory)
        {
            var v1StatusId = await _v1.GetStatusIdFromName(JiraSettings.GetInstance().GetV1StatusFromMapping(jiraInstance.InstanceUrl, jiraInstance.JiraProject, jiraStory.Fields.Status.Name));
            var story = jiraStory.ToV1Story(jiraInstance.V1Project, JiraSettings.GetInstance().GetV1PriorityIdFromMapping(jiraInstance.InstanceUrl, jiraStory.Fields.Priority.Name), v1StatusId);

            if (!string.IsNullOrEmpty(jiraStory.Fields.EpicLink))
            {
                var epic = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraStory.Fields.EpicLink);
                if (epic != null)
                {
                    if (epic.IsClosed)
                    {
                        _log.Error("Unable to assign epic " + jiraStory.Fields.EpicLink + " -- Epic may be closed");
                        return false;
                    }
                    story.Super = epic.Token;
                }
            }

            if (jiraStory.HasAssignee())
            {
                var member = await TrySyncMemberFromJiraUser(jiraStory.Fields.Assignee);
                if (member != null)
                    story.OwnersIds.Add(member.Oid());
            }

            _log.TraceFormat("Attempting to create story from Jira story {0}", jiraStory.Key);

            var newStory = await _v1.CreateStory(story);
            _log.DebugFormat("Created {0} from Jira story {1}", newStory.Number, jiraStory.Key);

            await _v1.RefreshBasicInfo(newStory);

            jiraInstance.UpdateIssue(newStory.ToIssueWithOnlyNumberAsLabel(jiraStory.Fields.Labels), jiraStory.Key);
            _log.TraceFormat("Updated labels on Jira story {0}", jiraStory.Key);

            jiraInstance.AddComment(jiraStory.Key, string.Format(CreatedFromV1Comment, newStory.Number, newStory.ScopeName));
            _log.TraceFormat("Added comment to Jira story {0}", jiraStory.Key);

            jiraInstance.AddWebLink(jiraStory.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, newStory.Number),
                        string.Format(V1AssetDetailWebLinkTitle, newStory.Number));
            _log.TraceFormat("Added web link to V1 story {0} on Jira story {1}", newStory.Number, jiraStory.Key);

            var link = new Uri(new Uri(jiraInstance.InstanceUrl), string.Format("browse/{0}", jiraStory.Key)).ToString();
            _v1.CreateLink(newStory, string.Format("Jira {0}", jiraStory.Key), link);
            _log.TraceFormat("Added link in V1 story {0}", newStory.Number);

            return true;
        }

        public void DeleteV1Stories(IJira jiraInstance, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Deleting stories started");
            var processedStories = 0;

            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Story => !v1Story.IsInactive && !string.IsNullOrWhiteSpace(v1Story.Reference))
                    .Select(v1Story => v1Story.Reference);

            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraStoryKey => !allJiraStories.Any(js => js.Key.Equals(jiraStoryKey))).ToList();

            if (jiraDeletedStoriesKeys.Any())
                _log.DebugFormat("Found {0} stories to check for delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                _log.TraceFormat("Attempting to delete V1 story referencing jira story {0}", key);
                _v1.DeleteStoryWithJiraReference(jiraInstance.V1Project, key);
                _log.DebugFormat("Deleted V1 story referencing jira story {0}", key);
                processedStories++;
            });

            if (processedStories > 0)
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
                _log.WarnFormat("Cannot get or create VersionOne Member for Jira User '{0}'", jiraUser.name);
                _log.Error(e);
            }

            return member;
        }
    }
}