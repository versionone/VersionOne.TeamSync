using System.Xml.Linq;

namespace VersionOne.TeamSync.JiraWorker.Domain.Xml
{
	public class V1NullableCDataSetNode : IV1ApiXmlNode
	{
		private readonly string _attributeName;
		private readonly string _value;

        public V1NullableCDataSetNode(string attributeName, string value)
		{
			_attributeName = attributeName;
			_value = value;
		}

		public void AddNode(XDocument doc)
		{
			var node = new XElement("Attribute", new XCData(_value));
			node.Add(
				new XAttribute("act", "set"),
				new XAttribute("name", _attributeName));

			doc.Root.Add(node);
		}
	}
}