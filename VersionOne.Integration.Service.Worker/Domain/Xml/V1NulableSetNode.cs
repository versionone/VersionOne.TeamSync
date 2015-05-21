using System.Xml.Linq;

namespace VersionOne.Integration.Service.Worker.Domain.Xml
{
	public class V1NullableSetNode : IV1ApiXmlNode
	{
		private readonly string _attributeName;
		private readonly string _value;

        public V1NullableSetNode(string attributeName, string value)
		{
			_attributeName = attributeName;
			_value = value;
		}

		public void AddNode(XDocument doc)
		{
			var node = new XElement("Attribute");
			node.Add(
				new XAttribute("act", "set"),
				new XAttribute("name", _attributeName));

            if (!string.IsNullOrWhiteSpace(_value))
			    node.Value = _value;
			doc.Root.Add(node);
		}
	}
}