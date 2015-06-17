﻿using System;
using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public class Defect : IPrimaryWorkItem
	{
		public string AssetType
		{
			get { return "Defect"; }
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
        public string ProjectName { get; set; }
        public string Super { get; set; }
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
                .AddSetRelationNode("Super", Super);
			return doc;
        }

        public XDocument CreateUpdatePayload()
        {
            var doc = XDocument.Parse("<Asset></Asset>");
            doc.AddSetNode("Name", Name)
                .AddSetRelationNode("Scope", ScopeId)
                .AddNullableSetNode("Description", Description)
                .AddNullableSetNode("Estimate", Estimate)
                .AddNullableSetNode("ToDo", ToDo)
                .AddSetNode("Reference", Reference);
            return doc;
        }

        internal void FromCreate(XElement asset)
        {
            ID = asset.GetAssetID();
        }

        public static Defect FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
            return new Defect()
            {
                ID = asset.GetAssetID(),
                Number = attributes.GetValueOrDefault("ID.Number"),
                ProjectName = attributes.GetValueOrDefault("Scope.Name"),
                Reference = attributes.GetValueOrDefault("Reference"),
                Description = attributes.GetPlainTextFromHtmlOrDefault("Description"),
                Name = attributes.GetValueOrDefault("Name"),
                IsInactive = Convert.ToBoolean(attributes.GetValueOrDefault("IsInactive")),
                AssetState = attributes.GetValueOrDefault("AssetState")
            };
        }

    }
}