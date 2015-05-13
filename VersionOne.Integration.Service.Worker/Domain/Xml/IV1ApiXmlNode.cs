using System.Xml.Linq;

namespace VersionOne.Integration.Service.Worker.Domain.Xml
{
	public interface IV1ApiXmlNode
	{
		void AddNode(XDocument doc);
	}
}