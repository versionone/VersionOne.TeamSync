using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.TfsConnector.Interfaces;

namespace VersionOne.TeamSync.TfsConnector.Connector
{
	public class TfsConnectorFactory : ITfsConnectorFactory
	{
		private IV1LogFactory _v1LogFactory;

		[ImportingConstructor]
		public TfsConnectorFactory([Import] IV1LogFactory v1LogFactory)
		{
			_v1LogFactory = v1LogFactory;
		}

		public ITfsConnector Create(TfsServer server)
		{
			return new TfsConnector(server, _v1LogFactory);
		}
	}
}