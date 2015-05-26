using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VersionOne.Integration.Service.Core;
using VersionOne.Integration.Service.Core.Config;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.SDK.APIClient;
using VersionOne.SDK.Jira.Config;
using VersionOne.SDK.Jira.Connector;

namespace VersionOne.Integration.Service.Worker
{
    public class VersionOneToJiraWorker
    {
        private IJira _jira;
        private IV1 _v1;
        private IV1Connector _v1Connector;

        private IDictionary<string, string> _v1ProjectToJiraProject;

        public VersionOneToJiraWorker()
        {
			_v1ProjectToJiraProject = new Dictionary<string, string>();
	        var firstServer = JiraSettings.Settings.Servers[0];

			for (var i = 0; i < firstServer.ProjectMappings.Count; i++)
				_v1ProjectToJiraProject.Add(firstServer.ProjectMappings[i].V1Project, firstServer.ProjectMappings[i].JiraProject);

			_jira = new Jira(new JiraConnector(firstServer.Url + "/rest/api/latest", firstServer.Username, firstServer.Password));
            _v1Connector = V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                .WithUserAgentHeader("guy", "15.0") //???? why
                .WithUsernameAndPassword(V1Settings.Settings.Username, V1Settings.Settings.Password)
                .Build();
        }

        public VersionOneToJiraWorker(IV1 v1, IJira jira, IDictionary<string,string> v1toJiraMappings)
        {
            _v1 = v1;
            _jira = jira;
	        _v1ProjectToJiraProject = v1toJiraMappings;
        }

        public VersionOneToJiraWorker(IV1Connector v1, IJiraConnector jiraConnector)
        {
            _v1Connector = v1;
            _jira = new Jira(jiraConnector);
        }

        public async void DoWork(TimeSpan serviceDuration)
        {
            SimpleLogger.WriteLogMessage("Beginning Output run... ");
            _v1 = new V1(_v1Connector, serviceDuration);

            await CreateEpics();
            await UpdateEpics();
            await ClosedV1EpicsSetJiraEpicsToResolved();
            await DeleteEpics();

            SimpleLogger.WriteLogMessage("Outpost run has finished");
        }

        public async Task DeleteEpics()
        {
            var deletedEpics = await _v1.GetDeletedEpics();
            deletedEpics.ForEach(epic =>
            {
                SimpleLogger.WriteLogMessage("Attempting to delete " + epic.Reference);

                _jira.DeleteEpicIfExists(epic.Reference);

                SimpleLogger.WriteLogMessage("Deleted " + epic.Reference);
                _v1.RemoveReferenceOnDeletedEpic(epic);
                
                SimpleLogger.WriteLogMessage("Removed reference on " + epic.Number);
            });

            SimpleLogger.WriteLogMessage("Total deleted epics processed was " + deletedEpics.Count);
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved()
        {
            var closedEpics = await _v1.GetClosedTrackedEpics();
            closedEpics.ForEach(epic =>
            {
                var jiraEpic = _jira.GetEpicByKey(epic.Reference);
                if (jiraEpic.HasErrors)
                {
                    //???
                    return;
                }
                _jira.SetIssueToResolved(epic.Reference);
            });
        }

        public async Task UpdateEpics()
        {
            var assignedEpics = await _v1.GetEpicsWithReference();
			var jiraEpics = _jira.GetEpicsInProjects(_v1ProjectToJiraProject.Values).issues;

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
                _jira.UpdateEpic(epic, relatedJiraEpic.Key);
                SimpleLogger.WriteLogMessage("Updated " + relatedJiraEpic.Key + " with data from " + epic.Number);
            });
        }

        public async Task CreateEpics()
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
                var jiraData = _jira.CreateEpic(epic, jiraProject);

				if (jiraData.IsEmpty)
					throw new InvalidDataException("Saving epic failed. Possible reasons : Jira project (" + jiraProject + ") doesn't have epic type or expected custom field");

                _jira.AddCreatedByV1Comment(jiraData.Key, epic, _v1.InstanceUrl);
                epic.Reference = jiraData.Key;
                _v1.UpdateEpicReference(epic);
                _v1.CreateLink(epic, "Jira Epic", _jira.InstanceUrl + "/browse/" + jiraData.Key);
            });
        }
    }
}
