using System;
using System.Collections.Generic;
using System.Globalization;
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
			return await _connector.Query("Epic", new[] { "ID.Number", "Name" }, new[] { "Reference=\"\"", "AssetState='Active'" }, Epic.FromQuery);
		}

		internal async void UpdateEpic(Epic epic)
		{
			epic.Description = DateTime.Now.ToString(CultureInfo.InvariantCulture);
			await _connector.Post(epic, epic.UpdateDescriptionXml());
		}
	}
}
