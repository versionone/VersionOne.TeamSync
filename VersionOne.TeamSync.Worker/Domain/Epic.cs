﻿using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public class Epic : IPrimaryWorkItem
    {
        public string AssetType
        {
            get { return "Epic"; }
        }

        public string ID { get; set; }
        public string Error { get; private set; }
        public bool HasErrors { get; private set; }
        public string ScopeId { get; set; }
        public string ScopeName { get; set; }

        public string Number { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AssetState { get; set; }
        public string Reference { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }

        public string CreateDateUTC { get; set; }
        public string ChangeDateUTC { get; set; }


        public static Epic FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
            return new Epic
            {
                ID = asset.GetAssetID(),
                Number = attributes.GetValueOrDefault("ID.Number"),
                Description = attributes.GetPlainTextFromHtmlOrDefault("Description"),
                Name = attributes.GetValueOrDefault("Name"),
                AssetState = attributes.GetValueOrDefault("AssetState"),
                Reference = attributes.GetValueOrDefault("Reference"),
                ScopeName = attributes.GetValueOrDefault("Scope.Name"),
                Priority = attributes.GetValueOrDefault("Priority.Name"),
                Status = attributes.GetValueOrDefault("Status.Name"),
                CreateDateUTC = attributes.GetValueOrDefault("CreateDateUTC"),
                ChangeDateUTC = attributes.GetValueOrDefault("ChangeDateUTC")
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
