using System.Linq;
using System.Xml.Linq;
using VersionOne.Integration.Service.Core.VersionOne.Interfaces;
using VersionOne.Integration.Service.Core.VersionOne.Xml;

namespace VersionOne.Integration.Service.Core.VersionOne
{
	public class Story : IV1Asset
	{
		public string Oid { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string AssetType { get { return "Story"; } }

		public string Reference { get; set; }
		public string Number { get; private set; }
		public string ID { get; private set; }
		public string Href { get; private set; }

		public Story() { }

		public Story(XElement doc)
		{
			ID = doc.GetAssetID();
			Href = doc.GetAssetHref();

			var attributes = doc.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);

			Name = attributes.GetValueOrDefault("Name");
			Description = attributes.GetValueOrDefault("Description");
			Reference = attributes.GetValueOrDefault("Reference");
			Number = attributes.GetValueOrDefault("Number");
		}

		public XDocument ToPostPayload()
		{
			var doc = XDocument.Parse("<Asset></Asset>");

			doc.AddSetNode("Reference", Reference)
				.AddSetNode("Description", Description);
			return doc;
		}

		//public Story GetStoryFromProject(VersionOneAPIConnector connector, string projectOid)
		//{
		//	var data = connector.GetData("rest-1.v1/Data/Story/1006?where=Scope=" + projectOid);
		//	return new Story(XDocument.Load(data).Root);
		//}

	}
}