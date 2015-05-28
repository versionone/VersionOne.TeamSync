using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VersionOne.Integration.Service.Core;
using VersionOne.Integration.Service.Core.Config;
using VersionOne.Integration.Service.Worker.Domain;
using System.Reflection;
using System.Xml.Linq;
using VersionOne.Api;
using VersionOne.Api.Interfaces;
using VersionOne.JiraConnector.Config;
using VersionOne.JiraConnector.Connector;

namespace VersionOne.Integration.Service.Worker
{
    public class VersionOneToJiraWorker
    {
        private List<IJira> _jiraInstances;
        private IV1 _v1;
        private IV1Connector _v1Connector;

        private IDictionary<string, string> _v1ProjectToJiraProject;

        public VersionOneToJiraWorker()
        {
			_v1ProjectToJiraProject = new Dictionary<string, string>();

            _jiraInstances = new List<IJira>();

            for (var i = 0; i < JiraSettings.Settings.Servers.Count; i++)
            {
                    var server = JiraSettings.Settings.Servers[i];
                if (!server.Enabled)
                    continue;

                for (var p = 0; p < server.ProjectMappings.Count; p++)
                {
                    if (server.ProjectMappings[p].Enabled)
                        _v1ProjectToJiraProject.Add(server.ProjectMappings[p].V1Project, server.ProjectMappings[p].JiraProject);
                }
                _jiraInstances.Add(new Jira(new JiraConnector.Connector.JiraConnector(new Uri(new Uri(server.Url), "/rest/api/latest").ToString(), server.Username, server.Password)));

            }

            switch (V1Settings.Settings.AuthenticationType)
            {
                case 0:
                    _v1Connector = V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithAccessToken(V1Settings.Settings.AccessToken)
                        .Build();
                    break;
                case 1:
                    _v1Connector = V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithUsernameAndPassword(V1Settings.Settings.Username, V1Settings.Settings.Password)
                        .Build();
                    break;
                default:
                    throw new Exception("Unsupported authentication type. Please check the VersionOne authenticationType setting in the config file.");
            }
        }

        public VersionOneToJiraWorker(IV1 v1, List<IJira> jira, IDictionary<string,string> v1toJiraMappings)
        {
            _v1 = v1;
            _jiraInstances = jira;
	        _v1ProjectToJiraProject = v1toJiraMappings;
        }

        public VersionOneToJiraWorker(IV1Connector v1, IJiraConnector jiraConnector)
        {
            _v1Connector = v1;
            _jiraInstances = new List<IJira> {new Jira(jiraConnector)};
        }

        public async void DoWork(TimeSpan serviceDuration)
        {
            SimpleLogger.WriteLogMessage("Beginning sync... ");
            _v1 = new V1(_v1Connector, serviceDuration);

            _jiraInstances.ForEach(async jira =>
            {
                await CreateEpics(jira);
                await UpdateEpics(jira);
                await ClosedV1EpicsSetJiraEpicsToResolved(jira);
                await DeleteEpics(jira);
            });

            SimpleLogger.WriteLogMessage("Ending sync...");
        }

        public async Task DeleteEpics(IJira jiraInstance)
        {
            var deletedEpics = await _v1.GetDeletedEpics();
            deletedEpics.ForEach(epic =>
            {
                SimpleLogger.WriteLogMessage("Attempting to delete " + epic.Reference);

                jiraInstance.DeleteEpicIfExists(epic.Reference);

                SimpleLogger.WriteLogMessage("Deleted " + epic.Reference);
                _v1.RemoveReferenceOnDeletedEpic(epic);
                
                SimpleLogger.WriteLogMessage("Removed reference on " + epic.Number);
            });

            SimpleLogger.WriteLogMessage("Total deleted epics processed was " + deletedEpics.Count);
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(IJira jiraInstance)
        {
            var closedEpics = await _v1.GetClosedTrackedEpics();
            closedEpics.ForEach(epic =>
            {
                var jiraEpic = jiraInstance.GetEpicByKey(epic.Reference);
                if (jiraEpic.HasErrors)
                {
                    //???
                    return;
                }
                jiraInstance.SetIssueToResolved(epic.Reference);
            });
        }

        public async Task UpdateEpics(IJira jiraInstance)
        {
            var assignedEpics = await _v1.GetEpicsWithReference();
            var searchResult = jiraInstance.GetEpicsInProjects(_v1ProjectToJiraProject.Values);

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
                jiraInstance.UpdateEpic(epic, relatedJiraEpic.Key);
                SimpleLogger.WriteLogMessage("Updated " + relatedJiraEpic.Key + " with data from " + epic.Number);
            });
        }

        public async Task CreateEpics(IJira jiraInstance)
        {
            var unassignedEpics = await _v1.GetEpicsWithoutReference();

            //if (unassignedEpics.Count > 0)
            //    SimpleLogger.WriteLogMessage("New epics found : " + string.Join(", ", unassignedEpics.Select(epic => epic.Number)));
            
            unassignedEpics.ForEach(epic =>
            {
	            if (!_v1ProjectToJiraProject.ContainsKey(epic.ProjectName))
	            {
		            //TODO : log, move on
		            return;
	            }

	            var jiraProject = _v1ProjectToJiraProject[epic.ProjectName];
                var jiraData = jiraInstance.CreateEpic(epic, jiraProject);

				if (jiraData.IsEmpty)
					throw new InvalidDataException("Saving epic failed. Possible reasons : Jira project (" + jiraProject + ") doesn't have epic type or expected custom field");

                jiraInstance.AddCreatedByV1Comment(jiraData.Key, epic, _v1.InstanceUrl);
                epic.Reference = jiraData.Key;
                _v1.UpdateEpicReference(epic);
                _v1.CreateLink(epic, "Jira Epic", jiraInstance.InstanceUrl + "/browse/" + jiraData.Key);
            });
        }
    }
}
