using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace VersionOne.TeamSync.Interfaces
{
    [InheritedExport]
    public interface IV1StartupWorkerFactory
    {
        IV1StartupWorker Create(CompositionContainer container);
    }
}