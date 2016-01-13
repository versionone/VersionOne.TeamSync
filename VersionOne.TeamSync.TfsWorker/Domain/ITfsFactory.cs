using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VersionOne.TeamSync.TfsConnector.Config;

namespace VersionOne.TeamSync.TfsWorker.Domain
{
    [InheritedExport]
    public interface ITfsFactory
    {
        IList<ITfs> Create(ITfsSettings tfsSettings);
    }
}