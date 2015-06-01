using System.Linq;
using System.Xml.Linq;
using VersionOne.Api.Extensions;
using VersionOne.Api.Interfaces;
using VersionOne.TeamSync.Service.Worker.Extensions;

namespace VersionOne.TeamSync.Service.Worker.Domain
{
	public class Epic : IV1Asset
	{
		public string AssetType
		{
			get { return "Epic"; }
		}

		public string ID { get; set; }
		public string Error { get; private set; }
		public bool HasErrors { get; private set; }
		public string Number { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string AssetState { get; set; }
		public string Reference { get; set; }
		public string ProjectName { get; set; }

		public static Epic FromQuery(XElement asset)
		{
			var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
			return new Epic()
			{
				ID = asset.GetAssetID(),
				Number = attributes.GetValueOrDefault("ID.Number"),
				Description = attributes.GetPlainTextFromHtmlOrDefault("Description"),
				Name = attributes.GetValueOrDefault("Name"),
				AssetState = attributes.GetValueOrDefault("AssetState"),

				Reference = attributes.GetValueOrDefault("Reference"),
				ProjectName = attributes.GetValueOrDefault("Scope.Name")
			};
		}

		public XDocument UpdateReferenceXml()
		{
			var doc = XDocument.Parse("<Asset></Asset>");
			doc.AddSetNode("Reference", Reference);
			return doc;
		}

		public XDocument UpdateDescriptionXml()
		{
			var doc = XDocument.Parse("<Asset></Asset>");
			doc.AddSetNode("Description", Description);
			return doc;
		}

		public XDocument RemoveReference()
		{
			Reference = string.Empty;
			var doc = XDocument.Parse("<Asset></Asset>");
			doc.AddNullableSetNode("Reference", Reference);
			return doc;
		}

        public bool IsClosed()
        {
            return AssetState == "128";
        }
    }
}
