using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.Integration.Service.Worker.Extensions;
using VersionOne.SDK.APIClient;

namespace VersionOne.Integration.Service.Worker.Domain
{
	public class V1
	{
		private readonly V1Connector _connector;
        private readonly string _aDayAgo = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd").InQuotes();
		public V1(V1Connector connector)
		{
			_connector = connector;
		}

		public async Task<List<Epic>> GetEpicsWithoutReference()
		{
		    var aDayAgo = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            return await _connector.Query("Epic", new[] { "ID.Number", "Name", "Description" }, new[] { "Reference=\"\"", "AssetState='Active'", "CreateDateUTC>=" + _aDayAgo }, Epic.FromQuery);
		}

        internal async void UpdateEpicReference(Epic epic)
        {
            await _connector.Post(epic, epic.UpdateReferenceXml());
        }

		internal async Task<List<Epic>> GetClosedTrackedEpics()
		{
			return await _connector.Query("Epic", new []{"Name", "AssetState"}, new[]{ "Reference!=\"\"", "AssetState='Closed'" }, Epic.FromQuery);
		}

        internal async Task<List<Epic>> GetEpicsWithReference()
        {
            return await _connector.Query("Epic", new[] { "ID.Number", "Name", "Description", "Reference" }, new[] { "Reference!=\"\"", "ChangeDateUTC>=" + _aDayAgo }, Epic.FromQuery);
        }
    }
}
