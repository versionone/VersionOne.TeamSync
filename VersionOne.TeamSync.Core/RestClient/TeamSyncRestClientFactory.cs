using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.TeamSync.Interfaces.RestClient;

namespace VersionOne.TeamSync.Core.RestClient
{
	public class TeamSyncRestClientFactory : ITeamSyncRestClientFactory
	{
		public ITeamSyncRestClient Create(TeamSyncRestClientSettings settings)
		{
			return new TeamSyncRestClient(settings);
		}
	}
}
