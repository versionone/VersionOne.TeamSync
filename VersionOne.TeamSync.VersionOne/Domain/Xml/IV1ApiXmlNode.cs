using System.Xml.Linq;

namespace VersionOne.TeamSync.VersionOneWorker.Domain.Xml
{
	public interface IV1ApiXmlNode
	{
		void AddNode(XDocument doc);
	}
}