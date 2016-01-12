using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.TeamSync.TfsConnector.Config;

namespace VersionOne.TeamSync.TfsConnector.Interfaces
{
	[InheritedExport]
	public interface ITfsConnectorFactory
	{
		ITfsConnector Create(TfsServer server);
	}
}
