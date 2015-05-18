using System.Collections.Generic;
using System.Linq.Expressions;
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

            unassignedEpics.ForEach(epic =>
            {
                _jira.CreateEpic(epic);
            });


            //if (unassignedEpics.Count > 0)
            //    _v1.UpdateEpic(unassignedEpics[0]);

            //var closedTrackedEpics = await _v1.GetClosedTrackedEpics();
        }
    }
}
