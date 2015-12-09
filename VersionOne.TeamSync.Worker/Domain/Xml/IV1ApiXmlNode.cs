using System.Xml.Linq;

namespace VersionOne.TeamSync.JiraWorker.Domain.Xml
{
	public interface IV1ApiXmlNode
	{
		void AddNode(XDocument doc);
	}
}