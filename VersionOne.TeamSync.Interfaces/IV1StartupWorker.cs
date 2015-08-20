using System;
namespace VersionOne.TeamSync.Interfaces
{
    public interface IV1StartupWorker
    {
        void DoWork();
        bool IsActualWorkEnabled { get; }
        void ValidateConnections();
    }
}
