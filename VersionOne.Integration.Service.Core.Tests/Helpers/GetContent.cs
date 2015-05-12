using System.IO;

namespace VersionOne.Integration.Service.Worker.Tests.Helpers
{
	public static class ContentResponses
	{
		public static class Jira
		{
			public static string ProjectSuccessful
			{
				get { return File.ReadAllText("ContentResponses\\GetProjectSuccessful.txt"); }
			}

			public static string FullIssue
			{
				get { return File.ReadAllText("ContentResponses\\FullIssue.txt"); }
			}
		}
	}
}
