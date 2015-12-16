namespace VersionOne.TeamSync.Interfaces
{
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