using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using log4net;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
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
        Task<Story> GetStoryWithJiraReference(string projectId, string jiraProjectKey);
        Task<Story> CreateStory(Story story);
        void ValidateConnection();

        Task<List<Story>> GetStoriesWithJiraReference(string projectId);
        Task RefreshBasicInfo(IPrimaryWorkItem workItem);
        Task<XDocument> UpdateAsset(IV1Asset asset, XDocument updateData);

        Task<string> GetAssetIdFromJiraReferenceNumber(string assetType, string assetIdNumber);

        Task CloseStory(string storyId);

        Task ReOpenStory(string storyId);
    }

    public class V1 : IV1
    {
		private readonly IV1Connector _connector;
	    private readonly string[] _numberNameDescriptRef = { "ID.Number", "Name", "Description", "Reference" };
        private const string _whereProject = "Scope=\"{0}\"";
        private const string _whereEpicCategory = "Category=\"{0}\"";
        private const int ConnectionAttempts = 3;
        private readonly string _aDayAgo;
        private static ILog _log = LogManager.GetLogger(typeof (V1));

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

        public async Task<Story> GetStoryWithJiraReference(string projectId, string jiraProjectKey)
        {
            var epic = await _connector.Query("Story", new[] {"ID.Number"}, new[] {"Reference=" + jiraProjectKey.InQuotes(), "Scope=" + projectId.InQuotes()}, Story.FromQuery);
            return epic.FirstOrDefault();
        }

        public async Task<Story> CreateStory(Story story)
        {
            var xDoc = await _connector.Post(story, story.CreatePayload());
            story.FromCreate(xDoc.Root);
            return story;
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

        public void ValidateConnection()
        {
            _log.Info("Verifying VersionOne connection...");
            _log.DebugFormat("URL: {0}", InstanceUrl);

            for (var i = 0; i < ConnectionAttempts; i++)
            {
                _log.DebugFormat("Connection attempt {0}.", i + 1);

                try
                {
                    if (!_connector.IsConnectionValid())
                    {
                        System.Threading.Thread.Sleep(5000);
                    }
                    else
                    {
                        _log.Info("VersionOne connection successful!");
                        return;
                    }
                }
                catch (Exception e)
                {
                    _log.Error("VersionOne connection failed.");
                    _log.Error(e.Message);
                    break;
                }
            }

            _log.Error("VersionOne connection failed.");
            throw new Exception(string.Format("Unable to validate connection to {0}.", InstanceUrl));
        }

        public async Task<List<Story>> GetStoriesWithJiraReference(string projectId)
        {
            return await _connector.Query("Story",
                new[] { "ID.Number", "Reference", "AssetState" },
                new[] { "Reference!=\"\"", string.Format(_whereProject, projectId) }, Story.FromQuery);
        }

        public async Task RefreshBasicInfo(IPrimaryWorkItem workItem)
        {
            await _connector.QueryOne(workItem.AssetType, workItem.ID, new[] {"ID.Number", "Scope.Name"}, (xElement) =>
            {
                var attributes = xElement.Elements("Attribute")
                    .ToDictionary(item => item.Attribute("name").Value, item => item.Value);
                workItem.ScopeName = attributes.GetValueOrDefault("Scope.Name");
                workItem.Number = attributes.GetValueOrDefault("ID.Number");
            });
        }

        public async Task<XDocument> UpdateAsset(IV1Asset asset, XDocument updateData)
        {
            return await _connector.Post(asset, updateData);
        }

        public async Task<string> GetAssetIdFromJiraReferenceNumber(string assetType, string jiraEpicKey)
        {
            var response = await _connector.Query(assetType, new[] {"ID"}, new[] {"Reference=" + jiraEpicKey.InQuotes()},
                element =>element.Attribute("id").Value);

            return response.FirstOrDefault();
        }

        public async Task CloseStory(string storyId)
        {
            await _connector.Operation("Story", storyId, "Inactivate");
        }

        public async Task ReOpenStory(string storyId)
        {
            await _connector.Operation("Story", storyId, "Reactivate");
        }
    }

    public interface IDateTime
    {
        DateTime UtcNow { get; }
    }
}
