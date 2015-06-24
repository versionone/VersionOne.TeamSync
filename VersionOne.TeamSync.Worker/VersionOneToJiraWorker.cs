using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker
{
    public class VersionOneToJiraWorker
    {
        private HashSet<V1JiraInfo> _jiraInstances;
        private IV1 _v1;
        private IV1Connector _v1Connector;
        private static ILog _log = LogManager.GetLogger(typeof(VersionOneToJiraWorker));

        public VersionOneToJiraWorker(TimeSpan serviceDuration)
        {
            _jiraInstances = new HashSet<V1JiraInfo>(V1JiraInfo.BuildJiraInfo(JiraSettings.Settings.Servers, serviceDuration.TotalMinutes.ToString(CultureInfo.InvariantCulture)));

            switch (V1Settings.Settings.AuthenticationType)
            {
                case 0:
                    _v1Connector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithAccessToken(V1Settings.Settings.AccessToken)
                        .Build();
                    break;
                case 1:
                    _v1Connector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithUsernameAndPassword(V1Settings.Settings.Username, V1Settings.Settings.Password)
                        .Build();
                    break;
                default:
                    throw new Exception("Unsupported authentication type. Please check the VersionOne authenticationType setting in the config file.");
            }

            _v1 = new V1(_v1Connector, serviceDuration);
        }

        public VersionOneToJiraWorker(IV1 v1)
        {
            _v1 = v1;
        }

        public async void DoWork()
        {
            _jiraInstances.ToList().ForEach(async jiraInfo =>
            {
                _log.Info("Beginning sync...");
                _log.Info("Syncing between " + jiraInfo.JiraKey + " and " + jiraInfo.V1ProjectId);

                await SyncEpics(jiraInfo);

                await DoStoryWork(jiraInfo); //this will be broken out to its own thing :-)
                await DoDefectWork(jiraInfo);
                _log.Info("Ending sync...");
            });
        }

        #region EPICS
        public async Task SyncEpics(V1JiraInfo jiraInfo)
        {
            _log.Info("Epic sync started...");
            await CreateEpics(jiraInfo);
            await UpdateEpics(jiraInfo);
            await ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
            await DeleteEpics(jiraInfo);
            _log.Info("Epic sync stopped...");
        }

        public async Task DeleteEpics(V1JiraInfo jiraInfo)
        {
            _log.Info("Delete epics started...");
            var processedEpics = 0;
            var deletedEpics = await _v1.GetDeletedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            _log.InfoFormat("Found {0} epics to delete", deletedEpics.Count);

            deletedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to delete Jira epic {0}", epic.Reference);

                jiraInfo.JiraInstance.DeleteEpicIfExists(epic.Reference);

                _log.DebugFormat("Deleted Jira epic {0}", epic.Reference);
                _v1.RemoveReferenceOnDeletedEpic(epic);

                _log.TraceFormat("Removed reference on {0}", epic.Number);
                processedEpics++;
            });

            _log.DebugFormat("Total epics deleted was {0}", processedEpics);
            _log.Trace("Delete epics stopped");
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(V1JiraInfo jiraInfo)
        {
            _log.Info("Resolving epics...");
            var processedEpics = 0;
            var closedEpics = await _v1.GetClosedTrackedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            _log.InfoFormat("Found {0} epics to resolve", closedEpics.Count);

            closedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to resolve Jira epic {0}", epic.Reference);
                var jiraEpic = jiraInfo.JiraInstance.GetEpicByKey(epic.Reference);
                if (jiraEpic.HasErrors)
                {
                    //???
                    _log.ErrorFormat("Jira epic {0} has errors", epic.Reference);
                    return;
                }
                jiraInfo.JiraInstance.SetIssueToResolved(epic.Reference);
                _log.DebugFormat("Resolved Jira epic {0}", epic.Reference);
                processedEpics++;
            });

            _log.InfoFormat("Total epics resolved was {0}", processedEpics);
            _log.Trace("Resolve epics stopped");
        }

        public async Task UpdateEpics(V1JiraInfo jiraInfo)
        {
            _log.Debug("Updating epics...");
            var processedEpics = 0;
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
            var searchResult = jiraInfo.JiraInstance.GetEpicsInProject(jiraInfo.JiraKey);

            if (searchResult.HasErrors)
            {
                searchResult.ErrorMessages.ForEach(_log.Error);
                return;
            }

            _log.InfoFormat("Found {0} epics to update", assignedEpics.Count);

            var jiraEpics = searchResult.issues;
            if (assignedEpics.Count > 0)
                _log.Trace("Recently updated epics : " + string.Join(", ", assignedEpics.Select(epic => epic.Number)));

            assignedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to update {0}", epic.Reference);
                var relatedJiraEpic = jiraEpics.FirstOrDefault(issue => issue.Key == epic.Reference);
                if (relatedJiraEpic == null)
                {
                    _log.Error("No related issue found in Jira for " + epic.Reference);
                    return;
                }

                if (relatedJiraEpic.Fields.Status.Name == "Done" && !epic.IsClosed()) //hrrmmm...
                    jiraInfo.JiraInstance.SetIssueToToDo(relatedJiraEpic.Key);

                jiraInfo.JiraInstance.UpdateIssue(epic.UpdateJiraEpic(), relatedJiraEpic.Key);
                _log.DebugFormat("Updated Jira epic {0} with data from V1 epic {1}", relatedJiraEpic.Key, epic.Number);
                processedEpics++;
            });

            _log.InfoFormat("Total epics updated was {0}", processedEpics);
            _log.Trace("Update epics stopped");
        }

        public async Task CreateEpics(V1JiraInfo jiraInfo)
        {
            _log.Info("Creating epics...");
            var processedEpics = 0;
            var unassignedEpics = await _v1.GetEpicsWithoutReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            _log.InfoFormat("Found {0} epics to create", unassignedEpics.Count);

            //if (unassignedEpics.Count > 0)
            //    SimpleLogger.WriteLogMessage("New epics found : " + string.Join(", ", unassignedEpics.Select(epic => epic.Number)));

            unassignedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to create Jira epic from {0}", epic.Number);
                var jiraData = jiraInfo.JiraInstance.CreateEpic(epic, jiraInfo.JiraKey);

                _log.DebugFormat("Created Jira epic {0} from V1 epic {1}", jiraData.Key, epic.Number);

                if (jiraData.IsEmpty)
                {
                    _log.ErrorFormat(
                        "Saving epic failed. Possible reasons : Jira project ({0}) doesn't have epic type or expected custom field",
                        jiraInfo.JiraKey);
                }
                else
                {
                    jiraInfo.JiraInstance.AddCreatedByV1Comment(jiraData.Key, epic.Number, epic.ScopeName,
                        _v1.InstanceUrl);
                    _log.TraceFormat("Added comment to {0}", jiraData.Key);
                    epic.Reference = jiraData.Key;
                    _v1.UpdateEpicReference(epic);
                    _log.TraceFormat("Added reference to V1 epic ({0})", epic.Number);
                    var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraData.Key;
                    _v1.CreateLink(epic, "Jira Epic", link);
                    _log.TraceFormat("Added link to V1 epic ({0})", epic.Number);
                    processedEpics++;
                }
            });

            _log.InfoFormat("Total epics created was {0}", processedEpics);
            _log.Trace("Create epics stopped");
        }
        #endregion EPICS

        public async Task CreateStoryFromJira(V1JiraInfo jiraInfo, Issue jiraStory)
        {
            var story = jiraStory.ToV1Story(jiraInfo.V1ProjectId);

            if (!string.IsNullOrEmpty(jiraStory.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraStory.Fields.EpicLink);
                story.Super = epicId;
            }

            var newStory = await _v1.CreateStory(story);

            await _v1.RefreshBasicInfo(newStory);

            jiraInfo.JiraInstance.UpdateIssue(newStory.ToIssueWithOnlyNumberAsLabel(jiraStory.Fields.Labels), jiraStory.Key);
            jiraInfo.JiraInstance.AddLinkToV1InComments(jiraStory.Key, newStory.Number, newStory.ProjectName,
                _v1.InstanceUrl);
        }

        public async Task UpdateStoryFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Story story)
        {
            if (issue.Fields.Status != null && issue.Fields.Status.Name != "Done" && story.AssetState == "128") //need to reopen a story first before we can update it
                await _v1.ReOpenStory(story.ID);

            var update = issue.ToV1Story(jiraInfo.V1ProjectId);

            update.ID = story.ID;

            await _v1.UpdateAsset(update, update.CreateUpdatePayload());

            if (issue.Fields.Status != null && issue.Fields.Status.Name == "Done" && story.AssetState != "128") //TODO : late bind? maybe??
                await _v1.CloseStory(story.ID);
        }

        public async Task DoStoryWork(V1JiraInfo jiraInfo)
        {
            var allJiraStories = jiraInfo.JiraInstance.GetStoriesInProject(jiraInfo.JiraKey).issues;
            _log.InfoFormat("Found {0} stories in Jira to process", allJiraStories.Count);
            var allV1Stories = await _v1.GetStoriesWithJiraReference(jiraInfo.V1ProjectId);

            UpdateStories(jiraInfo, allJiraStories, allV1Stories);

            CreateStories(jiraInfo, allJiraStories, allV1Stories);

            DeleteV1Stories(jiraInfo, allJiraStories, allV1Stories);
        }

        public void CreateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            var newStories = allJiraStories.Where(jStory =>
            {
                if (allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vStory => !string.IsNullOrWhiteSpace(vStory.Reference) &&
                                                              vStory.Reference.Contains(jStory.Key)) == null;
            }).ToList();

            newStories.ForEach(newJStory => CreateStoryFromJira(jiraInfo, newJStory));
        }

        public void UpdateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            var existingStories =
                allJiraStories.Where(jStory => { return allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            existingStories.ForEach(
                existingJStory =>
                    UpdateStoryFromJiraToV1(jiraInfo, existingJStory,
                        allV1Stories.Single(x => existingJStory.Fields.Labels.Contains(x.Number))));
        }

        public void DeleteV1Stories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Story => !v1Story.IsInactive && !string.IsNullOrWhiteSpace(v1Story.Reference))
                    .Select(v1Story => v1Story.Reference);
            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraStoryKey => !allJiraStories.Any(js => js.Key.Equals(jiraStoryKey))).ToList();

            jiraDeletedStoriesKeys.ForEach(key => _v1.DeleteStoryWithJiraReference(jiraInfo.V1ProjectId, key));
        }

        public void ValidateConnections()
        {
            _v1.ValidateConnection();

            foreach (var jiraInstance in _jiraInstances)
            {
                _log.InfoFormat("Verifying Jira connection...");
                _log.DebugFormat("URL: {0}", jiraInstance.JiraInstance.InstanceUrl);
                jiraInstance.ValidateConnection();
            }
        }

        //defect stuff
        public async Task CreateDefectFromJira(V1JiraInfo jiraInfo, Issue jiraDefect)
        {
            var defect = jiraDefect.ToV1Defect(jiraInfo.V1ProjectId);

            if (!string.IsNullOrEmpty(jiraDefect.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraDefect.Fields.EpicLink);
                defect.Super = epicId;
            }

            var newDefect = await _v1.CreateDefect(defect);

            await _v1.RefreshBasicInfo(newDefect);

            jiraInfo.JiraInstance.UpdateIssue(newDefect.ToIssueWithOnlyNumberAsLabel(jiraDefect.Fields.Labels), jiraDefect.Key);
            jiraInfo.JiraInstance.AddLinkToV1InComments(jiraDefect.Key, newDefect.Number, newDefect.ProjectName,
                _v1.InstanceUrl);
        }

        public async Task UpdateDefectFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Defect defect)
        {
            if (issue.Fields.Status != null && issue.Fields.Status.Name != "Done" && defect.AssetState == "128") //need to reopen a Defect first before we can update it
                await _v1.ReOpenDefect(defect.ID);

            var update = issue.ToV1Defect(jiraInfo.V1ProjectId);
            update.ID = defect.ID;

            await _v1.UpdateAsset(update, update.CreateUpdatePayload());

            if (issue.Fields.Status != null && issue.Fields.Status.Name == "Done" && defect.AssetState != "128") //TODO : late bind? maybe??
                await _v1.CloseDefect(defect.ID);
        }

        public async Task DoDefectWork(V1JiraInfo jiraInfo)
        {
            var allJiraDefects = jiraInfo.JiraInstance.GetDefectsInProject(jiraInfo.JiraKey).issues;
            _log.InfoFormat("Found {0} defects in Jira to process", allJiraDefects.Count);
            var allV1Defects = await _v1.GetDefectsWithJiraReference(jiraInfo.V1ProjectId);

            UpdateDefects(jiraInfo, allJiraDefects, allV1Defects);

            CreateDefects(jiraInfo, allJiraDefects, allV1Defects);

            DeleteV1Defects(jiraInfo, allJiraDefects, allV1Defects);
        }

        public void CreateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            var newStories = allJiraStories.Where(jDefect =>
            {
                if (allV1Stories.Any(x => jDefect.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vDefect => !string.IsNullOrWhiteSpace(vDefect.Reference) &&
                                                              vDefect.Reference.Contains(jDefect.Key)) == null;
            }).ToList();

            newStories.ForEach(newJDefect => CreateDefectFromJira(jiraInfo, newJDefect));
        }

        public void UpdateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            var existingStories =
                allJiraStories.Where(jDefect => { return allV1Stories.Any(x => jDefect.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            existingStories.ForEach(
                existingJDefect =>
                    UpdateDefectFromJiraToV1(jiraInfo, existingJDefect,
                        allV1Stories.Single(x => existingJDefect.Fields.Labels.Contains(x.Number))));
        }

        public void DeleteV1Defects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Defect => !v1Defect.IsInactive && !string.IsNullOrWhiteSpace(v1Defect.Reference))
                    .Select(v1Defect => v1Defect.Reference);
            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraDefectKey => !allJiraStories.Any(js => js.Key.Equals(jiraDefectKey))).ToList();

            jiraDeletedStoriesKeys.ForEach(key => _v1.DeleteDefectWithJiraReference(jiraInfo.V1ProjectId, key));
        }

    }

}
