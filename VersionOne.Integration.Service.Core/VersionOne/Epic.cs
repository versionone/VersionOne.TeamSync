using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersionOne.Integration.Service.Core.VersionOne.Interfaces;
using VersionOne.Integration.Service.Core.VersionOne.Xml;

namespace VersionOne.Integration.Service.Core.VersionOne
{
	public class Epic : IV1Asset
	{
		public string AssetType { get { return "Epic"; } }
		public string ID { get; private set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Href { get; set; }
		public string Reference { get; set; }
		public string Number { get; set; }
		public string Status { get; set; }
		public string Scope { get; set; }

		public Epic() { }

		public Epic(XElement doc)
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
				.AddSetNode("Name", Name)
				.AddSetNode("Description", Description)
				.AddSetRelationNode("Scope", Scope);
				
			return doc;

		}

		public static async Task<List<Epic>> GetEpicsOfTypeFeature(string scopeOid, VersionOneApi api)
		{
			var epic = api.Query("Epic", 
				new[] { "Name", "Number" },
				new[]
				{
					"Category.Name='Feature'",
					"Scope='Scope:1003'"
				},
				element => new Epic(element));

			return await epic;
		}

	}
}