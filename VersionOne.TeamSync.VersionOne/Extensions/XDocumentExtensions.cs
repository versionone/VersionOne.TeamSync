using System.Collections.Generic;
using System.Xml.Linq;
using VersionOne.TeamSync.VersionOne.Domain.Xml;

namespace VersionOne.TeamSync.VersionOne.Extensions
{
	public static class XDocumentExtensions
	{
		public static XDocument AddSetNode(this XDocument doc, string attributeName, string value)
		{
			var node = new V1SetNode(attributeName, value);
			node.AddNode(doc);
			return doc;
		}

		public static XDocument AddSetRelationNode(this XDocument doc, string attributeName, string value)
		{
			var node = new V1SetRelationNode(attributeName, value);
			node.AddNode(doc);
			return doc;
		}

        public static XDocument AddMultiRelationNode(this XDocument doc, string attributeName,IDictionary<string, string> values)
        {
            var node = new V1MultiRelationNode(attributeName, values);
            node.AddNode(doc);
            return doc;
        }

		public static XDocument AddNullableSetRelationNode(this XDocument doc, string attributeName, string value)
		{
			var node = new V1NullableSetRelationNode(attributeName, value);
			node.AddNode(doc);
			return doc;
		}

        public static XDocument AddNullableSetNode(this XDocument doc, string attributeName, string value)
        {
            var node = new V1NullableSetNode(attributeName, value);
            node.AddNode(doc);
            return doc;
        }

        public static XDocument AddNullableCDataSetNode(this XDocument doc, string attributeName, string value)
        {
            var node = new V1NullableCDataSetNode(attributeName, value);
            node.AddNode(doc);
            return doc;
        }
    }
}
