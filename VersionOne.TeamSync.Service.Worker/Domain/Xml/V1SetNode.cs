using System.Xml.Linq;

namespace VersionOne.TeamSync.Service.Worker.Domain.Xml
{
	public class V1SetNode : IV1ApiXmlNode
	{
		private readonly string _attributeName;
		private readonly string _value;

		public V1SetNode(string attributeName, string value)
		{
			_attributeName = attributeName;
			_value = value;
		}

		public void AddNode(XDocument doc)
		{
			if (string.IsNullOrWhiteSpace(_value))
				return;
			var node = new XElement("Attribute");
			node.Add(
				new XAttribute("act", "set"),
				new XAttribute("name", _attributeName));
			node.Value = _value;
			doc.Root.Add(node);
		}
	}
}