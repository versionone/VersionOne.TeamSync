using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public class BasicAsset
    {
        public string ID { get; set; }
        public string AssetState { get; set; }
        public bool IsClosed { get { return AssetState == "128"; } }

        public static BasicAsset FromQuery(XElement asset)
        {
            var attributes = asset.Elements("Attribute").ToDictionary(item => item.Attribute("name").Value, item => item.Value);
            return new BasicAsset()
            {
                ID = asset.GetAssetID(),
                AssetState = attributes.GetValueOrDefault("AssetState"),
            };
        }
    }
}
