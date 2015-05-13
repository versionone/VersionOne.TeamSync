using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.SDK.APIClient;

namespace VersionOne.Integration.Service.Worker.Domain
{
	public class V1
	{
		private readonly V1Connector _connector;

		public V1(V1Connector connector)
		{
			_connector = connector;
		}

		public async Task<List<Epic>> GetEpicsWithoutReference()
		{
			return await _connector.Query("Epic", new[] {"ID.Number", "Name"}, new[]{ "Reference=\"\"" }, Epic.FromQuery);
		}
	}
}
