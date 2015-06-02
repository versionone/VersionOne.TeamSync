using System.Xml.Linq;

namespace VersionOne.TeamSync.Worker.Domain.Xml
{
	public interface IV1ApiXmlNode
	{
		void AddNode(XDocument doc);
	}
}