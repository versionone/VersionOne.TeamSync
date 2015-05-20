using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VersionOne.Integration.Service.Core;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.SDK.APIClient;
using VersionOne.SDK.Jira.Connector;

namespace VersionOne.Integration.Service.Worker
{
    public class VersionOneToJiraWorker
    {
        private Jira _jira;
        private V1 _v1;

        public VersionOneToJiraWorker()
        {
            _jira = new Jira(new JiraConnector("http://jira-6.cloudapp.net:8080/rest/api/latest", ***REMOVED***));
            _v1 = new V1(
                V1Connector.WithInstanceUrl("http://localhost/VersionOne/")
                    .WithUserAgentHeader("guy", "15.0") //???? why
                    .WithUsernameAndPassword(***REMOVED***)
                    .Build()
                );
        }

        public async void DoWork()
        {
            SimpleLogger.WriteLogMessage("Beginning Output run... ");

            await CreateEpics();
            await UpdateEpics();
            await ClosedV1EpicsSetJiraEpicsToResolved();
            await DeleteEpics();

            SimpleLogger.WriteLogMessage("Outpost run has finished");
        }

        private async Task DeleteEpics()
        {
            var deletedEpics = await _v1.GetDeletedEpics();
            deletedEpics.ForEach(epic =>
            {
                SimpleLogger.WriteLogMessage("Attempting to delete " + epic.Reference);

                _jira.DeleteEpicIfExists(epic.Reference);

                SimpleLogger.WriteLogMessage("Deleted " + epic.Reference);

                //epic.Reference = String.Empty;
                //_v1.UpdateEpicReference(epic);
            });

            SimpleLogger.WriteLogMessage("Total deleted epics processed was " + deletedEpics.Count);
        }

        private async Task ClosedV1EpicsSetJiraEpicsToResolved()
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

        private async Task UpdateEpics()
        {
            var assignedEpics = await _v1.GetEpicsWithReference();
            var jiraEpics = _jira.GetEpicsInProject("OPC").issues;

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

        private async Task CreateEpics()
        {
            var unassignedEpics = await _v1.GetEpicsWithoutReference();

            if (unassignedEpics.Count > 0)
                SimpleLogger.WriteLogMessage("New epics found : " + string.Join(", ", unassignedEpics.Select(epic => epic.Number)));
            
            unassignedEpics.ForEach(epic =>
            {
                var jiraData = _jira.CreateEpic(epic, "OPC");
                _jira.AddCreatedByV1Comment(jiraData.Key, epic, _v1.Project, _v1.InstanceUrl);
                epic.Reference = jiraData.Key;
                _v1.UpdateEpicReference(epic);
            });
        }
    }
}
