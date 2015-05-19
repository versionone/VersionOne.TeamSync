using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
            var unassignedEpics = await _v1.GetEpicsWithoutReference();

            //CreateEpics(unassignedEpics);

            //await UpdateEpics();

            var closedEpics = await _v1.GetClosedTrackedEpics();
            closedEpics.ForEach(epic =>
            {
                var jiraEpic = _jira.GetEpicByKey(epic.Reference);
                if (jiraEpic.HasErrors)
                { //???
                    return;
                }
                _jira.SetIssueToResolved(epic.Reference);
            });
        }

        private async Task UpdateEpics()
        {
            var assignedEpics = await _v1.GetEpicsWithReference();
            var jiraEpics = _jira.GetEpicsInProject("OPC").issues;

            assignedEpics.ForEach(epic =>
            {
                var relatedJiraEpic = jiraEpics.FirstOrDefault(issue => issue.Key == epic.Reference);
                if (relatedJiraEpic != null)
                    _jira.UpdateEpic(epic, relatedJiraEpic.Key);
            });
        }

        private void CreateEpics(List<Epic> unassignedEpics)
        {
            unassignedEpics.ForEach(epic =>
            {
                var jiraData = _jira.CreateEpic(epic, "OPC");
                epic.Reference = jiraData.Key;
                _v1.UpdateEpicReference(epic);
            });
        }
    }
}
