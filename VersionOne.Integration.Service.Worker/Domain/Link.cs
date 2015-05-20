using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersionOne.Integration.Service.Worker.Extensions;
using VersionOne.SDK.APIClient.Model.Interfaces;

namespace VersionOne.Integration.Service.Worker.Domain
{
    public class Link : IVersionOneAsset
    {
        public string AssetType { get { return "Link"; }}
        public string ID { get; private set; }

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
