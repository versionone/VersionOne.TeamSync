using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.VersionOne.Extensions;

namespace VersionOne.TeamSync.VersionOne.Domain
{
    public class Member : IV1Asset
    {
        public string AssetType
        {
            get { return "Member"; }
        }

        public string ID { get; set; }
        public string Error { get; private set; }
        public bool HasErrors { get; private set; }

        public string Name { get; set; }
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }

        public XDocument CreatePayload()
        {
            return XDocument.Parse("<Asset></Asset>")
                .AddSetRelationNode("DefaultRole", "Role:12")
                .AddSetNode("IsCollaborator", "false")
                .AddSetNode("Name", Name)
                .AddSetNode("Nickname", Nickname)
                .AddSetNode("NotifyViaEmail", "false")
                .AddSetNode("SendConversationEmails", "false")
                .AddSetNode("Email", Email)
                .AddSetNode("Description", "Member account created by VersionOne TeamSync.");
        }

        public XDocument CreateUpdatePayload()
        {
            var doc = XDocument.Parse("<Asset></Asset>");
            doc.AddSetNode("Name", Name)
                .AddSetNode("Nickname", Nickname)
                .AddNullableSetNode("Email", Email);
            return doc;
        }

        public void FromCreate(XElement asset)
        {
            ID = asset.GetAssetID();
        }

        public static Member FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);

            return new Member
            {
                ID = asset.GetAssetID(),
                Name = attributes.GetValueOrDefault("Name"),
                Nickname = attributes.GetValueOrDefault("Nickname"),
                Username = attributes.GetValueOrDefault("Username"),
                Email = attributes.GetValueOrDefault("Email")
            };
        }
    }
}