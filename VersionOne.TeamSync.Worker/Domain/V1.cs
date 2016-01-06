using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using log4net;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.V1Connector;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public interface IV1
    {
        string InstanceUrl { get; }
        string MemberId { get; }
        bool ValidateConnection();
        bool ValidateProjectExists(string projectId);
        //bool ValidateScheduleExists(string projectId); D-09877
        bool ValidateEpicCategoryExists(string epicCategoryId);
        bool ValidateActualReferenceFieldExists();
        bool ValidateMemberPermissions();
        Task<string> GetPriorityId(string asset, string name);

        void CreateLink(IV1Asset asset, string title, string url);
        Task<BasicAsset> GetAssetIdFromJiraReferenceNumber(string assetType, string assetIdNumber);

        Task<XDocument> UpdateAsset(IV1Asset asset, XDocument updateData);

        Task<List<Epic>> GetEpicsWithoutReference(string projectId, string category);
        Task<List<Epic>> GetClosedTrackedEpicsUpdatedSince(string projectId, string category, DateTime updatedDate);
        Task<List<Epic>> GetEpicsWithReferenceUpdatedSince(string projectId, string category, DateTime updatedDate);
        Task<List<Epic>> GetDeletedEpicsUpdatedSince(string projectId, string category, DateTime updatedDate);
        Task<Epic> GetReferencedEpic(string v1Project, string epicCategory, string reference);
        void UpdateEpicReference(Epic epic);
        void RemoveReferenceOnDeletedEpic(Epic epic);

        Task<List<Story>> GetStoriesWithJiraReference(string projectId);
        Task<Story> CreateStory(Story story);
        Task RefreshBasicInfo(IPrimaryWorkItem workItem);
        void DeleteStory(string projectId, Story story);
        Task CloseStory(string storyId);
        Task ReOpenStory(string storyId);

        Task<List<Defect>> GetDefectsWithJiraReference(string projectId);
        Task<Defect> CreateDefect(Defect defect);
        Task CloseDefect(string defectId);
        Task ReOpenDefect(string defectId);
        void DeleteDefect(string projectId, Defect defect);

        Task<IEnumerable<Actual>> GetWorkItemActuals(string projectId, string workItemId);
        Task<Actual> CreateActual(Actual actual);

        // D-09877
        //Task<XDocument> CreateScheduleForProject(string projectId);
        //Task<XDocument> SetScheduleToProject(string projectId, string scheduleId);

        Task<Member> GetMember(string jiraUsername);
        Task<Member> CreateMember(Member member);
        Task<Member> SyncMemberFromJiraUser(User jiraUser);

        Task<List<Story>> GetStoriesWithJiraReferenceCreatedSince(string projectId, DateTime createdDate);
        Task<List<Defect>> GetDefectsWithJiraReferenceCreatedSince(string projectId, DateTime createdDate);
        Task<List<Epic>> GetEpicsWithoutReferenceCreatedSince(string v1Project, string epicCategory, DateTime createdDate);
        Task<List<Epic>> GetEpicsWithoutReferenceUpdatedSince(string v1Project, string epicCategory, DateTime updatedDate);

        Task<string> GetStatusIdFromName(string name);
    }

    public class V1 : IV1
    {
        private const int ProjectLeadOrder = 3;
        private const int ConnectionAttempts = 3;
        private const string WhereProject = "Scope=\"{0}\"";
        private const string WhereEpicCategory = "Category=\"{0}\"";
        private const string WhereReference = "Reference=\"{0}\"";
        private const string CreateOnUTC_before = "CreateDateUTC>='{0}'";
        private const string UpdateOnUTC_before = "ChangeDateUTC>='{0}'";

        private static readonly ILog Log = LogManager.GetLogger(typeof(V1));
        private readonly string[] _numberNameDescriptRef = { "ID.Number", "Name", "Description", "Reference" };

        private IV1Connector _connector;

        public V1()
        {
            BuildConnectorFromConfig();
        }

        public V1(IV1Connector v1Connector)
        {
            _connector = v1Connector;
        }

        public string InstanceUrl
        {
            get { return _connector.InstanceUrl; }
        }

        public string MemberId { get; private set; }

        public async Task<List<Epic>> GetEpicsWithoutReference(string projectId, string category)
        {
            return await _connector.Query("Epic",
                new[] { "ID.Number", "Name", "Description", "Scope.Name", "Priority.Name", "Status.Name" },
                new[]
                {
                    string.Format(WhereReference, ""),
                    "AssetState='Active'",
                    string.Format(WhereProject, projectId),
                    string.Format(WhereEpicCategory, category)
                }, Epic.FromQuery);
        }

        public async Task<List<Epic>> GetEpicsWithoutReferenceCreatedSince(string v1Project, string epicCategory, DateTime createdDate)
        {
            return await _connector.Query("Epic",
                new[] { "ID.Number", "Name", "Description", "Scope.Name", "Priority.Name" },
                new[]
                {
                    string.Format(WhereReference, ""),
                    "AssetState='Active'",
                    string.Format(WhereProject, v1Project),
                    string.Format(WhereEpicCategory, epicCategory),
                    string.Format(CreateOnUTC_before, createdDate.ToString(CultureInfo.InvariantCulture))
                }, Epic.FromQuery);
        }

        public async Task<List<Epic>> GetEpicsWithoutReferenceUpdatedSince(string v1Project, string epicCategory, DateTime updatedDate)
        {
            return await _connector.Query("Epic",
                new[] { "ID.Number", "Name", "Description", "Scope.Name", "Priority.Name", "ChangeDateUTC", "CreateDateUTC" },
                new[]
                {
                    string.Format(WhereReference, ""),
                    "AssetState='Active'",
                    string.Format(WhereProject, v1Project),
                    string.Format(WhereEpicCategory, epicCategory),
                    string.Format(UpdateOnUTC_before, updatedDate.ToString(CultureInfo.InvariantCulture))
                }, Epic.FromQuery);
        }

        public async Task<Epic> GetReferencedEpic(string v1Project, string epicCategory, string reference)
        {
            var epics = await _connector.Query("Epic",
                new[] { "ID.Number", "Name", "Description", "Scope.Name", "Priority.Name" },
                new[]
                {
                    "AssetState='Active'",
                    string.Format(WhereProject, v1Project),
                    string.Format(WhereEpicCategory, epicCategory),
                    string.Format(WhereReference, reference)
                }, Epic.FromQuery);
            
            return epics.FirstOrDefault();
        }

        public async void UpdateEpicReference(Epic epic)
        {
            await _connector.Post(epic, epic.UpdateReferenceXml());
        }

        public async Task<List<Epic>> GetClosedTrackedEpicsUpdatedSince(string projectId, string category, DateTime updatedDate)
        {
            return await _connector.Query("Epic", new[] { "Name", "AssetState", "Reference" },
                new[] { 
                    "Reference!=\"\"",
                    "AssetState='Closed'", 
                    string.Format(WhereProject, projectId),
                    string.Format(WhereEpicCategory, category),
                    string.Format(UpdateOnUTC_before, updatedDate.ToString(CultureInfo.InvariantCulture))
                }, Epic.FromQuery);
        }

        public async Task<List<Epic>> GetEpicsWithReferenceUpdatedSince(string projectId, string category, DateTime updatedDate)
        {
            return await _connector.Query("Epic", new[] { "ID.Number", "Name", "Description", "Reference", "AssetState", "Priority.Name", "Status.Name" },
                new[] { 
                    "Reference!=\"\"", 
                    string.Format(WhereProject, projectId), 
                    string.Format(WhereEpicCategory, category),
                    string.Format(UpdateOnUTC_before, updatedDate.ToString(CultureInfo.InvariantCulture))
                }, Epic.FromQuery);
        }

        public async Task<List<Epic>> GetDeletedEpicsUpdatedSince(string projectId, string category, DateTime updatedDate)
        {
            return await _connector.Query("Epic", _numberNameDescriptRef,
                new[] { 
                    "Reference!=\"\"", 
                    "IsDeleted='True'",
                    string.Format(WhereProject, projectId), 
                    string.Format(WhereEpicCategory, category),
                    string.Format(UpdateOnUTC_before, updatedDate.ToString(CultureInfo.InvariantCulture))
                }, Epic.FromQuery);
        }

        public async Task<Story> CreateStory(Story story)
        {
            var xDoc = await _connector.Post(story, story.CreatePayload());
            story.FromCreate(xDoc.Root);
            return story;
        }

        public async Task<Defect> CreateDefect(Defect defect)
        {
            var xDoc = await _connector.Post(defect, defect.CreatePayload());
            defect.FromCreate(xDoc.Root);
            return defect;
        }

        public async void DeleteStory(string projectId, Story story)
        {
            await _connector.Post(story, story.RemoveReference());
            await _connector.Operation(story, "Delete");
        }

        public async void DeleteDefect(string projectId, Defect defect)
        {
            await _connector.Post(defect, defect.RemoveReference());
            await _connector.Operation(defect, "Delete");
        }

        public async Task<string> GetPriorityId(string asset, string name)
        {
            var result = await _connector.Query(asset, new[] { "" }, new[] { string.Format("Name='{0}'", name) }, element => element.Attribute("id").Value);
            return result.FirstOrDefault();
        }

        public async void CreateLink(IV1Asset asset, string title, string url)
        {
            var link = new Link
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

        public bool ValidateConnection()
        {
            for (var i = 0; i < ConnectionAttempts; i++)
            {
                Log.DebugFormat("Connection attempt {0}.", i + 1);

                try
                {
                    if (_connector.IsConnectionValid())
                    {
                        MemberId = _connector.MemberId;
                        return true;
                    }
                    System.Threading.Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                    Log.Error("VersionOne connection failed.");
                    Log.Error(e.Message);
                    return false;
                }
            }
            return false;
        }

        public bool ValidateProjectExists(string projectId)
        {
            var result = _connector.Query("Scope", new[] { "Name" }, new[] { string.Format("ID='{0}'", projectId) },
                element =>
                {
                    return element.Elements("Attribute").Where(e => e.Attribute("name") != null && e.Attribute("name").Value.Equals("Name")).Select(e => e.Value).SingleOrDefault();
                }).Result;

            return result.Any() && !string.IsNullOrEmpty(result.SingleOrDefault());
        }

        //public bool ValidateScheduleExists(string projectId)
        //{
        //    var result = _connector.Query("Scope", new[] { "Schedule" }, new[] { string.Format("ID='{0}'", projectId) },
        //        element =>
        //        {
        //            return element.Elements("Attribute").Where(e => e.Attribute("name") != null && e.Attribute("name").Value.Equals("Schedule.Name")).Select(e => e.Value).SingleOrDefault();
        //        }).Result;

        //    return result.Any() && !string.IsNullOrEmpty(result.SingleOrDefault());
        //}

        public bool ValidateEpicCategoryExists(string epicCategoryId)
        {
            var result = _connector.Query("EpicCategory", new[] { "Name" }, new[] { string.Format("ID='{0}'", epicCategoryId) },
                element =>
                {
                    return element.Elements("Attribute").Where(e => e.Attribute("name") != null && e.Attribute("name").Value.Equals("Name")).Select(e => e.Value).SingleOrDefault();
                }).Result;

            return result.Any() && !string.IsNullOrEmpty(result.SingleOrDefault());
        }

        public bool ValidateActualReferenceFieldExists()
        {
            return _connector.AssetFieldExists("Actual", "Reference");
        }

        public bool ValidateMemberPermissions()
        {
            var defaultRoleOrder =
                _connector.Query("Member", new[] { "DefaultRole.Order" }, new[] { "IsSelf='True'" },
                    element => element.Descendants("Attribute").First().Value).Result.First();

            return Convert.ToInt32(defaultRoleOrder) <= ProjectLeadOrder;
        }

        public async Task<List<Story>> GetStoriesWithJiraReference(string projectId)
        {
            return await GetStoriesWithJiraReference(new[] { "Reference!=\"\"", string.Format(WhereProject, projectId) });
        }

        private async Task<List<Story>> GetStoriesWithJiraReference(string[] whereStrings)
        {
            return await _connector.Query("Story",
                new[] { "ID.Number", "Name", "Description", "Estimate", "ToDo", "Reference", "IsInactive", "AssetState", "Super.Number", "Priority", "Owners", "Status" },
                whereStrings,
                Story.FromQuery);
        }

        public async Task<List<Story>> GetStoriesWithJiraReferenceCreatedSince(string projectId, DateTime createdDate)
        {
            return await GetStoriesWithJiraReference(new[] { 
                "Reference!=\"\"", 
                string.Format(WhereProject, projectId),
                string.Format(CreateOnUTC_before, createdDate.ToString(CultureInfo.InvariantCulture))
            });
        }

        public async Task<List<Defect>> GetDefectsWithJiraReference(string projectId)
        {
            return await GetDefects(new[]
            {
                "Reference!=\"\"", 
                string.Format(WhereProject, projectId)
            });
        }

        private async Task<List<Defect>> GetDefects(string[] whereClauses)
        {
            return await _connector.Query("Defect",
                new[] { "ID.Number", "Name", "Description", "Estimate", "ToDo", "Reference", "IsInactive", "AssetState", "Super.Number", "Priority", "Owners", "Status" },
                whereClauses, Defect.FromQuery);
        }

        public async Task<List<Defect>> GetDefectsWithJiraReferenceCreatedSince(string projectId, DateTime createdDate)
        {
            return await GetDefects(new[] { 
                "Reference!=\"\"", 
                string.Format(WhereProject, projectId),
                string.Format(CreateOnUTC_before, createdDate.ToString(CultureInfo.InvariantCulture))
            });
        }

        public async Task RefreshBasicInfo(IPrimaryWorkItem workItem)
        {
            await _connector.QueryOne(workItem.AssetType, workItem.ID, new[] { "ID.Number", "Scope.Name" }, xElement =>
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

        public async Task<BasicAsset> GetAssetIdFromJiraReferenceNumber(string assetType, string jiraEpicKey)
        {
            var response = await _connector.Query(assetType,
                new[] { "ID", "AssetState" },
                new[] { "Reference=" + jiraEpicKey.InQuotes() },
                BasicAsset.FromQuery);

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

        public async Task CloseDefect(string defectId)
        {
            await _connector.Operation("Defect", defectId, "Inactivate");
        }

        public async Task ReOpenDefect(string defectId)
        {
            await _connector.Operation("Defect", defectId, "Reactivate");
        }

        public async Task<IEnumerable<Actual>> GetWorkItemActuals(string projectId, string workItemId)
        {
            return await _connector.Query("Actual",
                new[] { "Date", "Value", "Reference", "Scope.Name", "Workitem.Name", "Workitem.Number", "Member" },
                new[]
                {
                    "Reference!=\"\"",
                    string.Format("Workitem=\"{0}\"", workItemId),
                    string.Format(WhereProject, projectId)
                }, Actual.FromQuery);
        }

        public async Task<Actual> CreateActual(Actual actual)
        {
            var xDoc = await _connector.Post(actual, actual.CreatePayload());
            actual.FromCreate(xDoc.Root);

            return actual;
        }

        // D-09877
        //public async Task<XDocument> CreateScheduleForProject(string projectId)
        //{
        //    var projectName = _connector.Query("Scope", new[] { "Name" }, new[] { string.Format("ID='{0}'", projectId) },
        //        element =>
        //        {
        //            return element.Elements("Attribute").Where(e => e.Attribute("name") != null && e.Attribute("name").Value.Equals("Name")).Select(e => e.Value).SingleOrDefault();
        //        }).Result.First();

        //    var payload = XDocument.Parse("<Asset></Asset>")
        //        .AddSetNode("Name", string.Format("{0} Schedule", projectName))
        //        .AddSetNode("TimeboxGap", "0")
        //        .AddSetNode("TimeboxLength", "2 Weeks")
        //        .AddSetNode("Description", "Created by TeamSync.");

        //    return await _connector.Post("Schedule", payload.ToString());
        //}

        // D-09877
        //public async Task<XDocument> SetScheduleToProject(string projectId, string scheduleId)
        //{
        //    var payload = XDocument.Parse("<Asset></Asset>")
        //        .AddSetRelationNode("Schedule", scheduleId);

        //    return await _connector.Post(projectId.Replace(':', '/'), payload.ToString());
        //}

        public async Task<Member> GetMember(string jiraUsername)
        {
            var members =
                await
                    _connector.Query("Member", new[] { "Name", "Nickname", "Email", "Username" },
                        new[] { string.Format("Nickname='{0}'|Username='{0}'", jiraUsername) }, Member.FromQuery);

            return members.FirstOrDefault();
        }

        public async Task<Member> CreateMember(Member member)
        {
            var xDoc = await _connector.Post(member, member.CreatePayload());
            member.FromCreate(xDoc.Root);
            return member;
        }

        public async Task<Member> SyncMemberFromJiraUser(User jiraUser)
        {
            var member = await GetMember(jiraUser.name);
            if (member != null && !jiraUser.ItMatchesMember(member))
            {
                member.Name = jiraUser.displayName;
                member.Nickname = jiraUser.name;
                member.Email = jiraUser.emailAddress;
                await _connector.Post(member, member.CreateUpdatePayload());
            }
            return member ?? await CreateMember(jiraUser.ToV1Member());
        }

        public async Task<string> GetStatusIdFromName(string name)
        {
            var result = await _connector.Query("StoryStatus", new[] { "" }, new[] { string.Format("Name='{0}'", name) }, element => element.Attribute("id").Value);
            if (!result.Any())
                result = await _connector.Query("EpicStatus", new[] { "" }, new[] { string.Format("Name='{0}'", name) }, element => element.Attribute("id").Value);

            return result.FirstOrDefault();
        }

        private void BuildConnectorFromConfig()
        {
            var anonymousConnector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString());

            ICanSetProxyOrGetConnector authConnector;
            switch (V1Settings.Settings.AuthenticationType)
            {
                case 0:
                    authConnector = (ICanSetProxyOrGetConnector)anonymousConnector
                        .WithAccessToken(V1Settings.Settings.AccessToken);
                    break;
                case 1:
                    authConnector = (ICanSetProxyOrGetConnector)anonymousConnector
                        .WithUsernameAndPassword(V1Settings.Settings.Username, V1Settings.Settings.Password);
                    break;
                case 2:
                    authConnector = anonymousConnector
                        .WithWindowsIntegrated()
                        .UseOAuthEndpoints();
                    break;
                case 3:
                    authConnector = anonymousConnector
                        .WithWindowsIntegrated(V1Settings.Settings.Username, V1Settings.Settings.Password)
                        .UseOAuthEndpoints();
                    break;
                case 4:
                    authConnector = anonymousConnector
                        .WithAccessToken(V1Settings.Settings.AccessToken)
                        .UseOAuthEndpoints();
                    break;

                default:
                    throw new Exception("Unsupported authentication type. Please check the VersionOne authenticationType setting in the config file.");
            }

            if (V1Settings.Settings.Proxy.Enabled)
            {
                authConnector = (ICanSetProxyOrGetConnector)authConnector.WithProxy(new ProxyProvider(new Uri(V1Settings.Settings.Proxy.Url),
                    V1Settings.Settings.Proxy.Username, V1Settings.Settings.Proxy.Password,
                    V1Settings.Settings.Proxy.Domain));
            }

            _connector = authConnector.Build();
        }
    }
}