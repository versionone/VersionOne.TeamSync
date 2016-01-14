using System.Collections.Generic;

namespace VersionOne.TeamSync.TfsWorker.Domain
{
    public interface ITfs
    {
        string InstanceUrl { get; }
        List<TfsProjectMapping> ProjectMappings { get; }
        
        bool ValidateConnection();
        bool ValidateProjectExists(string projectName);
        bool ValidateMemberPermissions();
    }
}