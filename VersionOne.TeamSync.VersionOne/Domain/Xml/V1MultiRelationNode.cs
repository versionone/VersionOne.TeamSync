using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace VersionOne.TeamSync.VersionOneWorker.Domain.Xml
{
    public class V1MultiRelationNode : IV1ApiXmlNode
    {
        private readonly string _attributeName;
		private readonly IDictionary<string, string> _values;

		public V1MultiRelationNode(string attributeName, IDictionary<string, string> values)
		{
			_attributeName = attributeName;
			_values = values;
		}

		public void AddNode(XDocument doc)
		{
			if (!_values.Any())
				return;
			var node = new XElement("Relation");

			node.Add(new XAttribute("name", _attributeName));
		    
            foreach (var value in _values)
		    {
			    var relation = new XElement("Asset");
                relation.Add(new XAttribute("act", value.Value), new XAttribute("idref", value.Key));
			    node.Add(relation);
		    }

			doc.Root.Add(node);
		}
    }
}