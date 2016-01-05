using System.ComponentModel.Composition;

namespace VersionOne.TeamSync.Interfaces
{
    [InheritedExport]
    public interface IV1StartupWorker
    {
        void DoFirstRun();
        void DoWork();
        bool IsActualWorkEnabled { get; }
        void ValidateConnections();
        void ValidateProjectMappings();
        void ValidateMemberAccountPermissions();
        void ValidatePriorityMappings();
        void ValidateStatusMappings();
    }
}