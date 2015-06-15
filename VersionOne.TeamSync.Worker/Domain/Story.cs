using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public class Story : IPrimaryWorkItem
	{
		public string AssetType
		{
			get { return "Story"; }
		}

		public string ID { get; set; }
		public string Error { get; private set; }
		public bool HasErrors { get; private set; }
        public string Name { get; set; }
        public string Number { get; set; }

        public string ScopeId { get; set; }
        public string ScopeName { get; set; }
        public string Description { get; set; }
        public string Estimate { get; set; }
        public string ToDo { get; set; }
        public string Reference { get; set; }
        public string ProjectName { get; set; }
        public string Super { get; set; }

        public XDocument CreatePayload()
        {
			var doc = XDocument.Parse("<Asset></Asset>");
			doc.AddSetNode("Name", Name)
                .AddSetRelationNode("Scope", ScopeId)
                .AddSetNode("Description", Description)
                .AddSetNode("Estimate", Estimate)
                .AddSetNode("ToDo", ToDo)
                .AddSetNode("Reference", Reference)
                .AddSetRelationNode("Super",Super);
			return doc;
        }

        internal void FromCreate(XElement asset)
        {
            ID = asset.GetAssetID();
        }

        public static Story FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
            return new Story()
            {
                ID = asset.GetAssetID(),
                Number = attributes.GetValueOrDefault("ID.Number"),
                ProjectName = attributes.GetValueOrDefault("Scope.Name")
                //Description = attributes.GetPlainTextFromHtmlOrDefault("Description"),
                //Name = attributes.GetValueOrDefault("Name"),
                //Reference = attributes.GetValueOrDefault("Reference"),
            };
        }

    }
}
