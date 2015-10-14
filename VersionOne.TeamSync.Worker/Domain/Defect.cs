using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public class Defect : IPrimaryWorkItem
    {
        public Defect()
        {
            OwnersIds = new List<string>();
        }

        public string AssetType
        {
            get { return "Defect"; }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Defect)obj);
        }

        protected bool Equals(Defect other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Description, other.Description) && string.Equals(Estimate, other.Estimate) && string.Equals(ToDo, other.ToDo) && string.Equals(Reference, other.Reference) && string.Equals(Super, other.Super);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Estimate != null ? Estimate.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ToDo != null ? ToDo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Reference != null ? Reference.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Super != null ? Super.GetHashCode() : 0);
                return hashCode;
            }
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
        public string Status { get; set; }

        public string Super { get; set; }
        public string SuperNumber { get; set; }
        public bool IsInactive { get; private set; }
        public IList<string> OwnersIds { get; set; }

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
                .AddSetRelationNode("Priority", Priority)
                .AddSetRelationNode("Status", Status)
                .AddMultiRelationNode("Owners", OwnersIds.ToDictionary(memberId => memberId, memberId => "add"));
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
                .AddSetRelationNode("Priority", Priority)
                .AddSetRelationNode("Status", Status);
            return doc;
        }

        public XDocument CreateOwnersPayload(string newOwnerOid = null)
        {
            var doc = XDocument.Parse("<Asset></Asset>");
            var dictionary = OwnersIds.ToDictionary(memberId => memberId, memberId => "remove");
            if (newOwnerOid != null)
                dictionary.Add(newOwnerOid, "add");
            doc.AddMultiRelationNode("Owners", dictionary);
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

        public static Defect FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
            var relation = asset.Elements("Relation").ToDictionary(item => item.Attribute("name").Value, item => item.Elements("Asset").Select(x => x.Attribute("idref").Value).ToList());

            return new Defect
            {
                ID = asset.GetAssetID(),
                Number = attributes.GetValueOrDefault("ID.Number"),
                ScopeName = attributes.GetValueOrDefault("Scope.Name"),
                Reference = attributes.GetValueOrDefault("Reference"),
                Estimate = attributes.GetValueOrDefault("Estimate"),
                ToDo = attributes.GetValueOrDefault("ToDo"),
                Description = attributes.GetValueOrDefault("Description"),
                Name = attributes.GetValueOrDefault("Name"),
                IsInactive = Convert.ToBoolean(attributes.GetValueOrDefault("IsInactive")),
                AssetState = attributes.GetValueOrDefault("AssetState"),
                SuperNumber = attributes.GetValueOrDefault("Super.Number"),
                Priority = relation.GetSingleRelationValueOrDefault("Priority"),
                Status = relation.GetSingleRelationValueOrDefault("Status"),
                OwnersIds = relation.GetMultipleRelationValueOrDefault("Owners")
            };
        }
    }
}