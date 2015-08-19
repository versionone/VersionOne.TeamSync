using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.TeamSync.V1Connector.Interfaces
{
    [InheritedExport]
    public interface IV1ConnectorFactory
    {
        ICanSetUserAgentHeader WithInstanceUrl(string versionOneInstanceUrl);
    }
}
