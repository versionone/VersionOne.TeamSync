using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersionOne.Integration.Service.Worker.Domain.Xml;

namespace VersionOne.Integration.Service.Worker.Extensions
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
	}
}
