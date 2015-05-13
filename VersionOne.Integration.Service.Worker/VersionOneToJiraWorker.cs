using System.Collections.Generic;
using System.Linq.Expressions;
using VersionOne.JiraConnector.Rest;
using VersionOne.SDK.APIClient;

namespace VersionOne.Integration.Service.Worker
{
    public class VersionOneToJiraWorker
    {
		private JiraRestProxy _jiraApi;
		private V1Connector _v1Api;

	    public VersionOneToJiraWorker()
	    {
			_jiraApi = new JiraRestProxy("http://jira-6.cloudapp.net:8080", ***REMOVED***);
			_v1Api = V1Connector.WithInstanceUrl("http://localhost/VersionOne/")
				.WithUserAgentHeader("guy","15.0") //???? why
				.WithUsernameAndPassword(***REMOVED***)
				.Build();
	    }

		public async void DoWork()
		{
			var result = await _v1Api.Query<dynamic>("Story", new[] {"Name"}, element => new
			{
				Name = element.Name
			});

		}

    }

}
