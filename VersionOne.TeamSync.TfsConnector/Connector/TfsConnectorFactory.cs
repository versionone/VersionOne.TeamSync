using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.Interfaces.RestClient;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.TfsConnector.Interfaces;

namespace VersionOne.TeamSync.TfsConnector.Connector
{
	public class TfsConnectorFactory : ITfsConnectorFactory
	{
		private IV1LogFactory _v1LogFactory;
		private ITeamSyncRestClientFactory _restClientFactory;

		[ImportingConstructor]
		public TfsConnectorFactory([Import] IV1LogFactory v1LogFactory, [Import]ITeamSyncRestClientFactory restClientFactory)
		{
			_v1LogFactory = v1LogFactory;
			_restClientFactory = restClientFactory;
		}

		public ITfsConnector Create(TfsServer server)
		{
			return new TfsConnector(server, _v1LogFactory, _restClientFactory);
		}
	}
}