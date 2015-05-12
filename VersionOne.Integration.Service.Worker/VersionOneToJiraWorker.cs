using VersionOne.Integration.Service.Core.VersionOne;
using VersionOne.JiraConnector.Rest;

namespace VersionOne.Integration.Service.Worker
{
    public class VersionOneToJiraWorker
    {
	    private VersionOneApi _v1Api;
		private JiraRestProxy _jiraApi;
	    public VersionOneToJiraWorker()
	    {
		    _v1Api = new VersionOneApi();
			_jiraApi = new JiraRestProxy("http://jira-6.cloudapp.net:8080", ***REMOVED***);
	    }

		public async void DoWork()
	    {
		    var epics = await Epic.GetEpicsOfTypeFeature("Scope:0", _v1Api);
			epics.ForEach(AddIfTheyDontExistInJira);
	    }

		private void AddIfTheyDontExistInJira(Epic epic)
		{
			if (string.IsNullOrWhiteSpace(epic.Reference))
			{
				
			}

		}
		
    }
}
