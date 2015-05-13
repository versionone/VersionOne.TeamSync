using System.Collections.Generic;
using System.Linq.Expressions;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.JiraConnector.Rest;
using VersionOne.SDK.APIClient;

namespace VersionOne.Integration.Service.Worker
{
    public class VersionOneToJiraWorker
    {
		private JiraRestProxy _jiraApi;
	    private V1 _v1;

	    public VersionOneToJiraWorker()
	    {
			_jiraApi = new JiraRestProxy("http://jira-6.cloudapp.net:8080", ***REMOVED***);
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
			if (unassignedEpics.Count > 0)
				_v1.UpdateEpic(unassignedEpics[0]);
		}
    }

}
