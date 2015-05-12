using VersionOne.JiraConnector.Rest;

namespace VersionOne.Integration.Service.Worker
{
    public class VersionOneToJiraWorker
    {
		private JiraRestProxy _jiraApi;
	    public VersionOneToJiraWorker()
	    {
			_jiraApi = new JiraRestProxy("http://jira-6.cloudapp.net:8080", ***REMOVED***);
	    }

		public async void DoWork()
	    {
	    }

    }
}
