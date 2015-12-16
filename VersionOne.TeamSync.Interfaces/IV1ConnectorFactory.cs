using System.ComponentModel.Composition;

namespace VersionOne.TeamSync.Interfaces
{
    [InheritedExport]
    public interface IV1ConnectorFactory
    {
        ICanSetUserAgentHeader WithInstanceUrl(string versionOneInstanceUrl);
    }
}