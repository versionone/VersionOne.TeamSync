﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker
{
    public class VersionOneToJiraWorker
    {
        private HashSet<V1JiraInfo> _jiraInstances;
        private IV1 _v1;
        private IV1Connector _v1Connector;
        private static ILog _log = LogManager.GetLogger(typeof(VersionOneToJiraWorker));

        public VersionOneToJiraWorker(TimeSpan serviceDuration)
        {
            _jiraInstances = new HashSet<V1JiraInfo>(V1JiraInfo.BuildJiraInfo(JiraSettings.Settings.Servers, serviceDuration.TotalMinutes.ToString(CultureInfo.InvariantCulture)));

            switch (V1Settings.Settings.AuthenticationType)
            {
                case 0:
                    _v1Connector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithAccessToken(V1Settings.Settings.AccessToken)
                        .Build();
                    break;
                case 1:
                    _v1Connector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithUsernameAndPassword(V1Settings.Settings.Username, V1Settings.Settings.Password)
                        .Build();
                    break;
                default:
                    throw new Exception("Unsupported authentication type. Please check the VersionOne authenticationType setting in the config file.");
            }

            _v1 = new V1(_v1Connector, serviceDuration);
        }

        public VersionOneToJiraWorker(IV1 v1)
        {
            _v1 = v1;
        }

        public async void DoWork()
        {
            _jiraInstances.ToList().ForEach(async jiraInfo =>
            {
                _log.Info("Beginning TeamSync(tm) between " + jiraInfo.JiraKey + " and " + jiraInfo.V1ProjectId);

                await CreateEpics(jiraInfo);
                await UpdateEpics(jiraInfo);
                await ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
                await DeleteEpics(jiraInfo);

                await DoStoryWork(jiraInfo); //this will be broken out to its own thing :-)
                _log.Info("Ending sync...");
            });
        }

        public async Task CreateStoryFromJira(V1JiraInfo jiraInfo, Issue jiraStory)
        {
            var story = jiraStory.ToV1Story(jiraInfo.V1ProjectId);

            if (!string.IsNullOrEmpty(jiraStory.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic",jiraStory.Fields.EpicLink);
                story.Super = epicId;
            }

            var newStory = await _v1.CreateStory(story);

            await _v1.RefreshBasicInfo(newStory);

            jiraInfo.JiraInstance.UpdateIssue(newStory.ToIssueWithOnlyNumberAsLabel(), jiraStory.Key);
            jiraInfo.JiraInstance.AddLinkToV1InComments(jiraStory.Key, newStory.Number, newStory.ProjectName,
                _v1.InstanceUrl);
        }

        public async Task UpdateStoryFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Story story)
        {
            if (issue.Fields.Status != null && issue.Fields.Status.Name != "Done" && story.AssetState == "128") //need to reopen a story first before we can update it
                await _v1.ReOpenStory(story.ID);

            var update = issue.ToV1Story(jiraInfo.V1ProjectId);
            update.ID = story.ID;

            await _v1.UpdateAsset(update, update.CreatePayload());

            if (issue.Fields.Status != null && issue.Fields.Status.Name == "Done" && story.AssetState != "128") //TODO : late bind? maybe??
                await _v1.CloseStory(story.ID);
        }

        public async Task DoStoryWork(V1JiraInfo jiraInfo)
        {
            var allJiraStories = jiraInfo.JiraInstance.GetStoriesInProject(jiraInfo.JiraKey, jiraInfo.Interval).issues;
            _log.InfoFormat("Found {0} stories in Jira to process", allJiraStories.Count);
            var allV1Stories = await _v1.GetStoriesWithJiraReference(jiraInfo.V1ProjectId);

            UpdateStories(jiraInfo, allJiraStories, allV1Stories);
            
            CreateStories(jiraInfo, allJiraStories, allV1Stories);
            
            DeleteV1Stories(jiraInfo, allJiraStories, allV1Stories);
        }

        public void CreateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            var newStories = allJiraStories.Where(jStory =>
            {
                if (allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vStory => !string.IsNullOrWhiteSpace(vStory.Reference) &&
                                                              vStory.Reference.Contains(jStory.Key)) == null;
            }).ToList();

            newStories.ForEach(newJStory => CreateStoryFromJira(jiraInfo, newJStory));
        }

        public void UpdateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            var existingStories =
                allJiraStories.Where(jStory => { return allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            existingStories.ForEach(
                existingJStory =>
                    UpdateStoryFromJiraToV1(jiraInfo, existingJStory,
                        allV1Stories.Single(x => existingJStory.Fields.Labels.Contains(x.Number))));
        }

        public void DeleteV1Stories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Story => !v1Story.IsInactive && !string.IsNullOrWhiteSpace(v1Story.Reference))
                    .Select(v1Story => v1Story.Reference);
            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraStoryKey => !allJiraStories.Any(js => js.Key.Equals(jiraStoryKey))).ToList();

            jiraDeletedStoriesKeys.ForEach(key => _v1.DeleteStoryWithJiraReference(jiraInfo.V1ProjectId, key));
        }

        public void ValidateConnections()
        {
            _v1.ValidateConnection();

            foreach (var jiraInstance in _jiraInstances)
            {
                _log.InfoFormat("Verifying Jira connection...");
                _log.DebugFormat("URL: {0}", jiraInstance.JiraInstance.InstanceUrl);
                jiraInstance.ValidateConnection();
            }
        }

        public async Task DeleteEpics(V1JiraInfo jiraInfo)
        {
            var deletedEpics = await _v1.GetDeletedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
            deletedEpics.ForEach(epic =>
            {
                _log.Info("Attempting to delete " + epic.Reference);

                jiraInfo.JiraInstance.DeleteEpicIfExists(epic.Reference);

                _log.Info("Deleted " + epic.Reference);
                _v1.RemoveReferenceOnDeletedEpic(epic);

                _log.Info("Removed reference on " + epic.Number);
            });

            _log.Info("Total deleted epics processed was " + deletedEpics.Count);
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(V1JiraInfo jiraInfo)
        {
            var closedEpics = await _v1.GetClosedTrackedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
            closedEpics.ForEach(epic =>
            {
                var jiraEpic = jiraInfo.JiraInstance.GetEpicByKey(epic.Reference);
                if (jiraEpic.HasErrors)
                {
                    //???
                    return;
                }
                jiraInfo.JiraInstance.SetIssueToResolved(epic.Reference);
            });
        }


        public async Task UpdateEpics(V1JiraInfo jiraInfo)
        {
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
            var searchResult = jiraInfo.JiraInstance.GetEpicsInProject(jiraInfo.JiraKey);

            if (searchResult.HasErrors)
            {
                searchResult.ErrorMessages.ForEach(_log.Error);
                return;
            }

            var jiraEpics = searchResult.issues;
            if (assignedEpics.Count > 0)
                _log.Info("Recently updated epics : " + string.Join(", ", assignedEpics.Select(epic => epic.Number)));

            assignedEpics.ForEach(epic =>
            {
                var relatedJiraEpic = jiraEpics.FirstOrDefault(issue => issue.Key == epic.Reference);
                if (relatedJiraEpic == null)
                {
                    _log.Info("No related issue found in Jira for " + epic.Reference);
                    return;
                }

                if (relatedJiraEpic.Fields.Status.Name == "Done" && !epic.IsClosed()) //hrrmmm...
                    jiraInfo.JiraInstance.SetIssueToToDo(relatedJiraEpic.Key);

                jiraInfo.JiraInstance.UpdateIssue(epic.UpdateJiraEpic(), relatedJiraEpic.Key);
                _log.Info("Updated " + relatedJiraEpic.Key + " with data from " + epic.Number);
            });
        }

        public async Task CreateEpics(V1JiraInfo jiraInfo)
        {
            var unassignedEpics = await _v1.GetEpicsWithoutReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            //if (unassignedEpics.Count > 0)
            //    SimpleLogger.WriteLogMessage("New epics found : " + string.Join(", ", unassignedEpics.Select(epic => epic.Number)));

            unassignedEpics.ForEach(epic =>
            {
                var jiraData = jiraInfo.JiraInstance.CreateEpic(epic, jiraInfo.JiraKey);

                if (jiraData.IsEmpty)
                    throw new InvalidDataException("Saving epic failed. Possible reasons : Jira project (" + jiraInfo.JiraKey + ") doesn't have epic type or expected custom field");

                jiraInfo.JiraInstance.AddCreatedByV1Comment(jiraData.Key, epic.Number, epic.ScopeName, _v1.InstanceUrl);
                epic.Reference = jiraData.Key;
                _v1.UpdateEpicReference(epic);
                _v1.CreateLink(epic, "Jira Epic", jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraData.Key);
            });
        }
    }
}
