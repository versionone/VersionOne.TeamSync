using System;
using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.V1Connector.Extensions;
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
        public string AssetState { get; set; }

        public string ScopeId { get; set; }
        public string ScopeName { get; set; }
        public string Description { get; set; }
        public string Estimate { get; set; }
        public string ToDo { get; set; }
        public string Reference { get; set; }
        public string Priority { get; set; }

        public string Super { get; set; }
        public string SuperNumber { get; set; }
        public bool IsInactive { get; private set; }

        public XDocument CreatePayload()
        {
            var doc = XDocument.Parse("<Asset></Asset>");
            doc.AddSetNode("Name", Name)
                .AddSetRelationNode("Scope", ScopeId)
                .AddSetNode("Description", Description)
                .AddSetNode("Estimate", Estimate)
                .AddSetNode("ToDo", ToDo)
                .AddSetNode("Reference", Reference)
                .AddSetRelationNode("Super", Super)
                .AddSetRelationNode("Priority", Priority);
            return doc;
        }

        public XDocument CreateUpdatePayload()
        {
            var doc = XDocument.Parse("<Asset></Asset>");
            doc.AddSetNode("Name", Name)
                .AddSetRelationNode("Scope", ScopeId)
                .AddNullableCDataSetNode("Description", Description)
                .AddNullableSetNode("Estimate", Estimate)
                .AddNullableSetNode("ToDo", ToDo)
                .AddNullableSetRelationNode("Super", Super)
                .AddSetNode("Reference", Reference)
                .AddSetRelationNode("Priority", Priority);
            return doc;
        }

        internal void FromCreate(XElement asset)
        {
            ID = asset.GetAssetID();
        }

        public XDocument RemoveReference()
        {
            Reference = string.Empty;
            var doc = XDocument.Parse("<Asset></Asset>");
            doc.AddNullableSetNode("Reference", Reference);
            return doc;
        }

        public static Story FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
            var priorityRelationNode = asset.Elements("Relation").SingleOrDefault(e => e.Attribute("name").Value.Equals("Priority"));
            var priority = priorityRelationNode != null ? priorityRelationNode.Element("Asset") : null;
            return new Story
            {
                ID = asset.GetAssetID(),
                Number = attributes.GetValueOrDefault("ID.Number"),
                ScopeName = attributes.GetValueOrDefault("Scope.Name"),
                Reference = attributes.GetValueOrDefault("Reference"),
                Description = attributes.GetValueOrDefault("Description"),
                Estimate = attributes.GetValueOrDefault("Estimate"),
                ToDo = attributes.GetValueOrDefault("ToDo"),
                Name = attributes.GetValueOrDefault("Name"),
                IsInactive = Convert.ToBoolean(attributes.GetValueOrDefault("IsInactive")),
                AssetState = attributes.GetValueOrDefault("AssetState"),
                SuperNumber = attributes.GetValueOrDefault("Super.Number"),
                Priority = priority != null ? priority.Attribute("idref").Value : string.Empty
            };
        }
    }
}
