using System.Xml.Linq;

namespace VersionOne.Integration.Service.Core.VersionOne.Xml
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

		public static bool HasAssets(this XDocument doc)
		{
			if (!doc.Root.HasAttributes)
				return false;

			var root = doc.Root;
			var total = int.Parse(root.Attribute("total").Value);
			return total > 0;
		}

		public static string GetAssetID(this XElement xElement)
		{
			if (xElement == null)
				return "";

			return xElement.Attribute("id").Value.Split(':')[1];
		}

		public static string GetAssetHref(this XElement xElement)
		{
			if (xElement == null)
				return "";

			return xElement.Attribute("href").Value;
		}
	}
}