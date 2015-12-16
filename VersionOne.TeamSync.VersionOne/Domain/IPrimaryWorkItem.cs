using VersionOne.TeamSync.Interfaces;

namespace VersionOne.TeamSync.VersionOne.Domain
{
    public interface IPrimaryWorkItem : IV1Asset
    {
        string ScopeId { get; set; }
        string ScopeName { get; set; }
        string Number { get; set; }
        string Reference { get; set; }
        string Priority { get; set; }
    }
}
