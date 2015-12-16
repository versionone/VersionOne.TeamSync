using System.Xml.Linq;

namespace VersionOne.TeamSync.VersionOne.Domain.Xml
{
	public interface IV1ApiXmlNode
	{
		void AddNode(XDocument doc);
	}
}