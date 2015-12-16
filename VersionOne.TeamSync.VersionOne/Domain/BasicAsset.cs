using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.VersionOne.Extensions;

namespace VersionOne.TeamSync.VersionOne.Domain
{
    public class BasicAsset
    {
        public string ID { get; set; }
        public string Token { get; set; }
        public string AssetState { get; set; }
        public bool IsClosed { get { return AssetState == "128"; } }

        public static BasicAsset FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
            return new BasicAsset()
            {
                ID = asset.GetAssetID(),
                Token = asset.GetToken(),
                AssetState = attributes.GetValueOrDefault("AssetState"),
            };
        }

    }
}
