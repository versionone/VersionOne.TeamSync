using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.TeamSync.Interfaces.RestClient
{
	[InheritedExport]
	public interface ITeamSyncRestClientFactory
	{
		ITeamSyncRestClient Create(TeamSyncRestClientSettings settings);
	}
}
