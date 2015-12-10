using VersionOne.TeamSync.V1Connector.Interfaces;

namespace VersionOne.TeamSync.VersionOneWorker.Extensions
{
    public static class V1AssetExtensions
    {
        public static string Oid(this IV1Asset asset)
        {
            return string.Format("{0}:{1}", asset.AssetType, asset.ID);
        }
    }
}