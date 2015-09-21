using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public class Actual : IV1Asset
    {
        public string AssetType
        {
            get { return "Actual"; }
        }

        public string ID { get; set; }
        public string Error { get; private set; }
        public bool HasErrors { get; private set; }

        public DateTime Date { get; set; }
        public string Value { get; set; }
        public string Reference { get; set; }
        public string MemberId { get; set; }
        public string ScopeId { get; set; }
        public string WorkItemId { get; set; }

        internal void FromCreate(XElement asset)
        {
            ID = asset.GetAssetID();
        }

        public XDocument CreatePayload()
        {
            return XDocument.Parse("<Asset></Asset>")
                .AddSetNode("Date", Date.ToString(CultureInfo.InvariantCulture))
                .AddSetNode("Value", Value)
                .AddSetNode("Reference", Reference)
                .AddSetRelationNode("Member", MemberId)
                .AddSetRelationNode("Scope", ScopeId)
                .AddSetRelationNode("Workitem", WorkItemId);
        }

        public static Actual FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);

            return new Actual
            {
                ID = asset.GetAssetID(),
                Date = DateTime.Parse(attributes.GetValueOrDefault("Date")),
                Value = attributes.GetValueOrDefault("Value"),
                Reference = attributes.GetValueOrDefault("Reference"),
                MemberId = asset.Elements("Relation").Where(e => e.Attribute("name").Value.Equals("Member")).Elements("Asset").Single().Attribute("idref").Value
            };
        }
    }
}
