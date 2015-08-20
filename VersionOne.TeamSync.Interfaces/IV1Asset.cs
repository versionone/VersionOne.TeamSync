using System.Xml.Linq;

namespace VersionOne.TeamSync.V1Connector.Interfaces
{
    public interface IV1Asset
    {
        string AssetType { get; }
        string ID { get; }

        string Error { get; }
        bool HasErrors { get; }
    }

}
