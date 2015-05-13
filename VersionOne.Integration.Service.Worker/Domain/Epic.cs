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

		public static Epic FromQuery(XElement asset)
		{
			var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
			return new Epic()
			{
				ID = asset.GetAssetID(),
				Number = attributes.GetValueOrDefault("ID.Number"),
				Name = attributes.GetValueOrDefault("Name")
			};
		}

		public XDocument UpdateDescriptionXml()
		{
			var doc = XDocument.Parse("<Asset></Asset>");
			doc.AddSetNode("Description", Description);
			return doc;
		}
	}
}
