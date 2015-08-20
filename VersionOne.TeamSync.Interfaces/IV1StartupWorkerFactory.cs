using System;
using System.ComponentModel.Composition;
namespace VersionOne.TeamSync.Interfaces
{
    [InheritedExport]
    public interface IV1StartupWorkerFactory
    {
        IV1StartupWorker Create(TimeSpan serviceDuration);
    }
}
