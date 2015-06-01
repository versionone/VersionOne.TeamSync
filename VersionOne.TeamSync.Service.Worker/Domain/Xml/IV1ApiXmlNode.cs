using System.Xml.Linq;

namespace VersionOne.TeamSync.Service.Worker.Domain.Xml
{
	public interface IV1ApiXmlNode
	{
		void AddNode(XDocument doc);
	}
}