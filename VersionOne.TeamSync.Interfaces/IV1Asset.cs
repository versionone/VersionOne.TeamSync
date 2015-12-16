namespace VersionOne.TeamSync.Interfaces
{
    public interface IV1Asset
    {
        string AssetType { get; }
        string ID { get; }

        string Error { get; }
        bool HasErrors { get; }
    }

}
