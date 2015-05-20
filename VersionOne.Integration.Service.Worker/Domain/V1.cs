using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.Integration.Service.Worker.Extensions;
using VersionOne.SDK.APIClient;
using VersionOne.SDK.APIClient.Model.Interfaces;

namespace VersionOne.Integration.Service.Worker.Domain
{
	public class V1
	{
		private readonly V1Connector _connector;
        private readonly string _aDayAgo = DateTime.UtcNow.AddSeconds(-15).ToString("yyyy-MM-dd").InQuotes();
	    private readonly string[] _numberNameDescriptRef = { "ID.Number", "Name", "Description", "Reference" };

	    public V1(V1Connector connector)
		{
			_connector = connector;

            //need properties from the connector for this
	        Project = "TODO: expose / supply project?";
	        InstanceUrl = "http://localhost/VersionOne/";
		}

        public string Project { get; private set; }
        public string InstanceUrl { get; private set; }

		public async Task<List<Epic>> GetEpicsWithoutReference()
		{
            return await _connector.Query("Epic", new[] { "ID.Number", "Name", "Description" }, new[] { "Reference=\"\"", "AssetState='Active'", "CreateDateUTC>=" + _aDayAgo }, Epic.FromQuery);
		}

        internal async void UpdateEpicReference(Epic epic)
        {
            await _connector.Post(epic, epic.UpdateReferenceXml());
        }

		internal async Task<List<Epic>> GetClosedTrackedEpics()
		{
            return await _connector.Query("Epic", new[] { "Name", "AssetState", "Reference" }, new[] { "Reference!=\"\"", "AssetState='Closed'", "ChangeDateUTC>=" + _aDayAgo }, Epic.FromQuery);
		}

        internal async Task<List<Epic>> GetEpicsWithReference()
        {
            return await _connector.Query("Epic", _numberNameDescriptRef, new[] { "Reference!=\"\"", "ChangeDateUTC>=" + _aDayAgo }, Epic.FromQuery);
        }

        internal async Task<List<Epic>> GetDeletedEpics()
        {
            return await _connector.Query("Epic", _numberNameDescriptRef, new[] { "Reference!=\"\"", "IsDeleted='True'", "ChangeDateUTC>=" + _aDayAgo }, Epic.FromQuery);
        }

        internal async void CreateLink(IVersionOneAsset asset, string title, string url)
        {
            var link = new Link()
            {
                Asset = asset.AssetType + ":" + asset.ID, //TODO: add a token
                OnMenu = true,
                Name = title,
                Url = url,
            };

            await _connector.Post(link, link.CreatePayload());
        }
    }
}
