using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker
{
    public class VersionOneToJiraWorker
    {
        private HashSet<V1JiraInfo> _jiraInstances;
        private IV1 _v1;
        private IV1Connector _v1Connector;

        public VersionOneToJiraWorker()
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
        }

        public VersionOneToJiraWorker(IV1 v1)
        {
            _v1 = v1;
        }

        public async void DoWork(TimeSpan serviceDuration)
        {
            _v1 = new V1(_v1Connector, serviceDuration);

            _jiraInstances.ToList().ForEach(async jiraInfo => 
            {
                SimpleLogger.WriteLogMessage("Beginning TeamSync(tm) between " + jiraInfo.JiraKey + " and " + jiraInfo.V1ProjectId);

                await CreateEpics(jiraInfo);
                await UpdateEpics(jiraInfo);
                await ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
                await DeleteEpics(jiraInfo);
                SimpleLogger.WriteLogMessage("Ending sync...");
            });
        }

        public async Task DeleteEpics(V1JiraInfo jiraInfo)
        {
            var deletedEpics = await _v1.GetDeletedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
            deletedEpics.ForEach(epic =>
            {
                SimpleLogger.WriteLogMessage("Attempting to delete " + epic.Reference);

                jiraInfo.JiraInstance.DeleteEpicIfExists(epic.Reference);

                SimpleLogger.WriteLogMessage("Deleted " + epic.Reference);
                _v1.RemoveReferenceOnDeletedEpic(epic);

                SimpleLogger.WriteLogMessage("Removed reference on " + epic.Number);
            });

            SimpleLogger.WriteLogMessage("Total deleted epics processed was " + deletedEpics.Count);
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
                searchResult.ErrorMessages.ForEach(SimpleLogger.WriteLogMessage);
                return;
            }

            var jiraEpics = searchResult.issues;
            if (assignedEpics.Count > 0)
                SimpleLogger.WriteLogMessage("Recently updated epics : " + string.Join(", ", assignedEpics.Select(epic => epic.Number)));

            assignedEpics.ForEach(epic =>
            {
                var relatedJiraEpic = jiraEpics.FirstOrDefault(issue => issue.Key == epic.Reference);
                if (relatedJiraEpic == null)
                {
                    SimpleLogger.WriteLogMessage("No related issue found in Jira for " + epic.Reference);
                    return;
                }

                if (relatedJiraEpic.Fields.Status.Name == "Done" && !epic.IsClosed()) //hrrmmm...
                    jiraInfo.JiraInstance.SetIssueToToDo(relatedJiraEpic.Key);
                
                jiraInfo.JiraInstance.UpdateEpic(epic, relatedJiraEpic.Key);
                SimpleLogger.WriteLogMessage("Updated " + relatedJiraEpic.Key + " with data from " + epic.Number);
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

                jiraInfo.JiraInstance.AddCreatedByV1Comment(jiraData.Key, epic, _v1.InstanceUrl);
                epic.Reference = jiraData.Key;
                _v1.UpdateEpicReference(epic);
                _v1.CreateLink(epic, "Jira Epic", jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraData.Key);
            });
        }
    }
}
