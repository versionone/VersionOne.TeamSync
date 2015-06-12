using VersionOne.TeamSync.V1Connector.Interfaces;

namespace VersionOne.TeamSync.Worker.Domain
{
    public interface IPrimaryWorkItem : IV1Asset
    {
        string ScopeId { get; set; }
        string ScopeName { get; set; }
        string Number { get; set; }
    }
}
