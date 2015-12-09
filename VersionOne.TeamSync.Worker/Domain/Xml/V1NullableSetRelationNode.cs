using System.Xml.Linq;

namespace VersionOne.TeamSync.JiraWorker.Domain.Xml
{
	public class V1NullableSetRelationNode : IV1ApiXmlNode
	{
		private readonly string _attributeName;
		private readonly string _value;

		public V1NullableSetRelationNode(string attributeName, string value)
		{
			_attributeName = attributeName;
			_value = value;
		}

		public void AddNode(XDocument doc)
		{
			var node = new XElement("Relation");

			node.Add(
				new XAttribute("act", "set"),
				new XAttribute("name", _attributeName));

			var relation = new XElement("Asset");

			if (!string.IsNullOrWhiteSpace(_value))
				relation.Add(new XAttribute("idref", _value));

			node.Add(relation);

			doc.Root.Add(node);

		}
	}
}