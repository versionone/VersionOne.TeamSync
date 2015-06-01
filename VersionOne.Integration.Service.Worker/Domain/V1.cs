using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VersionOne.Api.Interfaces;
using VersionOne.Integration.Service.Worker.Extensions;

namespace VersionOne.Integration.Service.Worker.Domain
{
    public interface IV1
    {
        string InstanceUrl { get; }
        Task<List<Epic>> GetEpicsWithoutReference(string projectId, string category);
        void UpdateEpicReference(Epic epic);
        Task<List<Epic>> GetClosedTrackedEpics(string projectId, string category);
        Task<List<Epic>> GetEpicsWithReference(string projectId, string category);
        Task<List<Epic>> GetDeletedEpics(string projectId, string category);
        void CreateLink(IV1Asset asset, string title, string url);
        void RemoveReferenceOnDeletedEpic(Epic epic);
    }

    public class V1 : IV1
    {
		private readonly IV1Connector _connector;
	    private readonly string[] _numberNameDescriptRef = { "ID.Number", "Name", "Description", "Reference" };
        private const string _whereProject = "Scope=\"{0}\"";
        private const string _whereEpicCategory = "Category=\"{0}\"";
        private readonly string _aDayAgo;

	    public V1(IV1Connector connector, IDateTime dateTime, TimeSpan serviceDuration)
		{
			_connector = connector;

            //need properties from the connector for this
	        InstanceUrl = _connector.InstanceUrl;
            _aDayAgo = dateTime.UtcNow.Add(-serviceDuration).ToString("yyyy-MM-dd HH:mm:ss").InQuotes();
		}

        public V1(IV1Connector connector, TimeSpan serviceDuration)
        {
            _connector = connector;

            //need properties from the connector for this
            InstanceUrl = _connector.InstanceUrl;
            _aDayAgo = DateTime.UtcNow.Add(-serviceDuration).ToString("yyyy-MM-dd HH:mm:ss").InQuotes();
        }

        public string InstanceUrl { get; private set; }

        public async Task<List<Epic>> GetEpicsWithoutReference(string projectId, string category)
        {
            return await _connector.Query("Epic",
                new[] {"ID.Number", "Name", "Description", "Scope.Name"},
                new[]
                {
                    "Reference=\"\"",
                    "AssetState='Active'",
                    //"CreateDateUTC>=" + _aDayAgo,
                    string.Format(_whereProject, projectId),
                    string.Format(_whereEpicCategory, category)
                }, Epic.FromQuery);
        }

	    public async void UpdateEpicReference(Epic epic)
        {
            await _connector.Post(epic, epic.UpdateReferenceXml());
        }

        public async Task<List<Epic>> GetClosedTrackedEpics(string projectId, string category)
		{
            return await _connector.Query("Epic", new[] { "Name", "AssetState", "Reference" }, 
                new[] { 
                    "Reference!=\"\"",
                    "AssetState='Closed'", 
                    //"ChangeDateUTC>=" + _aDayAgo, 
                    string.Format(_whereProject, projectId),
                    string.Format(_whereEpicCategory, category)
                }, Epic.FromQuery);
		}

	    public async Task<List<Epic>> GetEpicsWithReference(string projectId, string category)
        {
            return await _connector.Query("Epic", new[] { "ID.Number", "Name", "Description", "Reference", "AssetState"},
                new[] { 
                    "Reference!=\"\"", 
                    //"ChangeDateUTC>=" + _aDayAgo, 
                    string.Format(_whereProject, projectId), 
                    string.Format(_whereEpicCategory, category)
                }, Epic.FromQuery);
        }

        public async Task<List<Epic>> GetDeletedEpics(string projectId, string category)
        {
            return await _connector.Query("Epic", _numberNameDescriptRef, 
                new[] { 
                    "Reference!=\"\"", 
                    "IsDeleted='True'",
                    //"ChangeDateUTC>=" + _aDayAgo, 
                    string.Format(_whereProject, projectId), 
                    string.Format(_whereEpicCategory, category) 
                }, Epic.FromQuery);
        }

        public async void CreateLink(IV1Asset asset, string title, string url)
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

        public async void RemoveReferenceOnDeletedEpic(Epic epic)
        {
            await _connector.Operation(epic, "Undelete");
            await _connector.Post(epic, epic.RemoveReference());
            await _connector.Operation(epic, "Delete");
        }
    }

    public interface IDateTime
    {
        DateTime UtcNow { get; }
    }
}
