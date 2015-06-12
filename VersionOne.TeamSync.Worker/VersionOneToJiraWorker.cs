using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            _jiraInstances = new HashSet<V1JiraInfo>(V1JiraInfo.BuildJiraInfo(JiraSettings.Settings.Servers));

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

                //await CreateEpics(jiraInfo);
                //await UpdateEpics(jiraInfo);
                //await ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
                //await DeleteEpics(jiraInfo);

                CreateStories(jiraInfo);
                _log.Info("Ending sync...");
            });
        }

        public void CreateStories(V1JiraInfo jiraInfo)
        {
            var searchResults = jiraInfo.JiraInstance.GetStoriesWithNoEpicInProject(jiraInfo.JiraKey);
            searchResults.issues.ForEach(async jiraStory =>
            {
                var existingStory = await _v1.GetStoryWithJiraReference(jiraInfo.V1ProjectId, jiraStory.Key);
                if (existingStory != null) return;
                var newStory = await _v1.CreateStory(jiraStory.ToV1Story(jiraInfo.V1ProjectId));
                jiraInfo.JiraInstance.UpdateIssue(newStory.ToIssueWithOnlyNumberAsLabel(), jiraStory.Key);
                jiraInfo.JiraInstance.AddLinkToV1InComments(jiraStory.Key, newStory.Number, newStory.ProjectName, _v1.InstanceUrl);
            });
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

                jiraInfo.JiraInstance.AddCreatedByV1Comment(jiraData.Key, epic.Number, epic.ProjectName, _v1.InstanceUrl);
                epic.Reference = jiraData.Key;
                _v1.UpdateEpicReference(epic);
                _v1.CreateLink(epic, "Jira Epic", jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraData.Key);
            });
        }
    }
}
