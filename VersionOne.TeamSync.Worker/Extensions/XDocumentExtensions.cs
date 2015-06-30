﻿using System.Xml.Linq;
using VersionOne.TeamSync.Worker.Domain.Xml;

namespace VersionOne.TeamSync.Worker.Extensions
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
