using System.IO;

namespace VersionOne.TeamSync.Core.Tests.Helpers
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

		public static class VersionOne
		{
			public static string BasicV1QueryResponse
			{
				get { return File.ReadAllText("ContentResponses\\VersionOne\\BasicV1QueryResponse.xml"); }
			}
			public static string BadAttributeResponse
			{
				get { return File.ReadAllText("ContentResponses\\VersionOne\\BadAttributeResponse.xml"); }
			}

		}
	}
}
