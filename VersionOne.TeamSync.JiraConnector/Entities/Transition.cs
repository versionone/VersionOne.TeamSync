using System.Collections.Generic;

namespace VersionOne.TeamSync.JiraConnector.Entities
{
	public class TransitionResponse : JiraBase
	{
		public string Expand { get; set; }
		public List<Transition> Transitions { get; set; }
	}

	public class Transition
	{
		public string Name { get; set; }
		public string Id { get; set; }
	}
}
