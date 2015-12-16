using VersionOne.TeamSync.Interfaces;

namespace VersionOne.TeamSync.VersionOne.Extensions
{
    public static class V1AssetExtensions
    {
        public static string Oid(this IV1Asset asset)
        {
            return string.Format("{0}:{1}", asset.AssetType, asset.ID);
        }
    }
}