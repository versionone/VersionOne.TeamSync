﻿using System.Xml.Linq;
using VersionOne.Api.Interfaces;
using VersionOne.Integration.Service.Worker.Extensions;

namespace VersionOne.Integration.Service.Worker.Domain
{
    public class Link : IV1Asset
    {
        public string AssetType { get { return "Link"; }}
        public string ID { get; private set; }
        public string Error { get; private set; }
        public bool HasErrors { get; private set; }

        public string Asset { get; set; }
        public string Name { get; set; }
        public bool OnMenu { get; set; }
        public string Url { get; set; }

        public XDocument CreatePayload()
        {
            var doc = XDocument.Parse("<Asset></Asset>");
            doc.AddSetNode("Name", Name)
                .AddSetNode("OnMenu", OnMenu ? "true" : "false")
                .AddSetNode("URL", Url)
                .AddSetRelationNode("Asset", Asset);
            return doc;
        }

    }
}
