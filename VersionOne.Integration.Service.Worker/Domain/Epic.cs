using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersionOne.Integration.Service.Worker.Extensions;
using VersionOne.SDK.APIClient.Extensions;
using VersionOne.SDK.APIClient.Model.Interfaces;

namespace VersionOne.Integration.Service.Worker.Domain
{
	public class Epic : IVersionOneAsset
	{
		public string AssetType
		{
			get { return "Epic"; }
		}

		public string ID { get; set; }
		public string Number { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string AssetState { get; set; }
        public string Reference { get; set; }

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

                Reference = attributes.GetValueOrDefault("Reference")
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
	}
}
