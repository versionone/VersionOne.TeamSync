using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        private IEnumerable<V1JiraInfo> _jiraInstances;
        private IV1 _v1;
        private static ILog _log = LogManager.GetLogger(typeof(VersionOneToJiraWorker));
        private static DateTime syncTime;

        public VersionOneToJiraWorker(TimeSpan serviceDuration)
        {
            IV1Connector v1Connector;
            switch (V1Settings.Settings.AuthenticationType)
            {
                case 0:
                    v1Connector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithAccessToken(V1Settings.Settings.AccessToken)
                        .Build();
                    break;
                case 1:
                    v1Connector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithUsernameAndPassword(V1Settings.Settings.Username, V1Settings.Settings.Password)
                        .Build();
                    break;
                default:
                    throw new Exception("Unsupported authentication type. Please check the VersionOne authenticationType setting in the config file.");
            }
            _v1 = new V1(v1Connector, serviceDuration);

            _jiraInstances = V1JiraInfo.BuildJiraInfo(JiraSettings.Settings.Servers, serviceDuration.TotalMinutes.ToString(CultureInfo.InvariantCulture));
        }

        public VersionOneToJiraWorker(IV1 v1)
        {
            _v1 = v1;
        }

        public void DoWork()
        {
            _log.Info("Beginning sync...");
            _jiraInstances.ToList().ForEach(async jiraInfo =>
            {
                syncTime = DateTime.Now;
                _log.Info("Syncing between " + jiraInfo.JiraKey + " and " + jiraInfo.V1ProjectId);

                await DoEpicWork(jiraInfo);
                await DoStoryWork(jiraInfo); //this will be broken out to its own thing :-)
                await DoDefectWork(jiraInfo);
            });
            _log.Info("Ending sync...");
            _log.DebugFormat("Total sync time: {0}", DateTime.Now - syncTime);
        }

        public void ValidateConnections()
        {
            _v1.ValidateConnection();

            foreach (var jiraInstance in _jiraInstances.ToList())
            {
                _log.InfoFormat("Verifying Jira connection...");
                _log.DebugFormat("URL: {0}", jiraInstance.JiraInstance.InstanceUrl);
                jiraInstance.ValidateConnection();
            }
        }

        public void ValidateProjectMappings()
        {
            foreach (var jiraInstance in _jiraInstances.ToList())
            {
                _log.InfoFormat("Verifying V1ProjectID={1} to JiraProjectID={0} project mapping...", jiraInstance.JiraKey, jiraInstance.V1ProjectId);

                if (jiraInstance.ValidateMapping(_v1))
                {
                    _log.Info("Mapping successful! Projects will be synchronized.");
                }
                else
                {
                    _log.Error("Mapping failed, projects will not be synchronized.");
                    ((HashSet<V1JiraInfo>)_jiraInstances).Remove(jiraInstance);
                }
            }
        }

        #region EPICS
        public async Task DoEpicWork(V1JiraInfo jiraInfo)
        {
            _log.Trace("Epic sync started...");
            await CreateEpics(jiraInfo);
            await UpdateEpics(jiraInfo);
            await ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
            await DeleteEpics(jiraInfo);
            _log.Trace("Epic sync stopped...");
        }

        public async Task DeleteEpics(V1JiraInfo jiraInfo)
        {
            _log.Trace("Deleting Jira epics...");
            var processedEpics = 0;
            var deletedEpics = await _v1.GetDeletedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            _log.DebugFormat("Found {0} epics to check for delete", deletedEpics.Count);

            deletedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to delete {0}", epic.Reference);

                jiraInfo.JiraInstance.DeleteEpicIfExists(epic.Reference);
                _log.DebugFormat("Deleted epic {0}", epic.Reference);

                _v1.RemoveReferenceOnDeletedEpic(epic);
                _log.TraceFormat("Removed reference on V1 epic {0}", epic.Number);

                processedEpics++;
            });

            _log.InfoFormat("Deleted {0} epics", processedEpics);
            _log.Trace("Delete Jira epics stopped");
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(V1JiraInfo jiraInfo)
        {
            _log.Trace("Resolving Jira epics...");
            var processedEpics = 0;
            var closedEpics = await _v1.GetClosedTrackedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            _log.TraceFormat("Found {0} epics to check for resolve", closedEpics.Count);

            closedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to resolve {0}", epic.Reference);
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

            _log.InfoFormat("Resolved {0} epics", processedEpics);
            _log.Trace("Resolve Jira epics stopped");
        }

        public async Task UpdateEpics(V1JiraInfo jiraInfo)
        {
            _log.Trace("Updating Jira epics...");
            var processedEpics = 0;
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
            var searchResult = jiraInfo.JiraInstance.GetEpicsInProject(jiraInfo.JiraKey);

            if (searchResult.HasErrors)
            {
                searchResult.ErrorMessages.ForEach(_log.Error);
                return;
            }
            var jiraEpics = searchResult.issues;

            assignedEpics.RemoveAll(epic => searchResult.issues.SingleOrDefault(epic.ItMatches) != null);
            _log.DebugFormat("Found {0} epics to check for update", assignedEpics.Count);

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

            _log.InfoFormat("Updated {0} epics", processedEpics);
            _log.Trace("Updating Jira epics stopped...");
        }

        public async Task CreateEpics(V1JiraInfo jiraInfo)
        {
            _log.Trace("Creating Jira epics...");
            var processedEpics = 0;
            var unassignedEpics = await _v1.GetEpicsWithoutReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            _log.DebugFormat("Found {0} epics to check for create", unassignedEpics.Count);

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
                    jiraInfo.JiraInstance.AddCreatedByV1Comment(jiraData.Key, epic.Number, epic.ScopeName, _v1.InstanceUrl);
                    _log.TraceFormat("Added comment to {0}", jiraData.Key);
                    epic.Reference = jiraData.Key;
                    _v1.UpdateEpicReference(epic);
                    _log.TraceFormat("Added reference in V1 epic {0}", epic.Number);
                    var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraData.Key;
                    _v1.CreateLink(epic, "Jira Epic", link);
                    _log.TraceFormat("Added link in V1 epic {0}", epic.Number);
                    processedEpics++;
                }
            });

            _log.InfoFormat("Created {0} epics", processedEpics);
            _log.Trace("Create Jira epics stopped");
        }
        #endregion EPICS

        #region STORIES
        public async Task DoStoryWork(V1JiraInfo jiraInfo)
        {
            _log.Trace("Story sync started...");
            var allJiraStories = jiraInfo.JiraInstance.GetStoriesInProject(jiraInfo.JiraKey).issues;
            var allV1Stories = await _v1.GetStoriesWithJiraReference(jiraInfo.V1ProjectId);

            UpdateStories(jiraInfo, allJiraStories, allV1Stories);
            CreateStories(jiraInfo, allJiraStories, allV1Stories);
            DeleteV1Stories(jiraInfo, allJiraStories, allV1Stories);
            _log.Trace("Story sync stopped...");
        }

        public void UpdateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Updating V1 stories...");
            var processedStories = 0;
            var existingStories =
                allJiraStories.Where(jStory => { return allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            _log.DebugFormat("Found {0} stories to check for update", existingStories.Count);

            existingStories.ForEach(async existingJStory =>
            {
                var story = allV1Stories.Single(x => existingJStory.Fields.Labels.Contains(x.Number));

                if (!existingJStory.ItMatchesStory(story))
                {
                    await UpdateStoryFromJiraToV1(jiraInfo, existingJStory, story);
                    processedStories++;
                }
            });

            _log.InfoFormat("Updated {0} stories", processedStories);
            _log.Trace("Updating V1 stories stopped...");
        }

        public async Task UpdateStoryFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Story story)
        {
            _log.TraceFormat("Attempting to update {0}", story.Number);

            //need to reopen a story first before we can update it
            if (issue.Fields.Status != null && issue.Fields.Status.Name != "Done" && story.AssetState == "128")
            {
                await _v1.ReOpenStory(story.ID);
                _log.DebugFormat("Reopened story {0}", story.Number);
            }

            var update = issue.ToV1Story(jiraInfo.V1ProjectId);

            update.ID = story.ID;

            await _v1.UpdateAsset(update, update.CreateUpdatePayload());
            _log.DebugFormat("Updated V1 story {0} from Jira story {1}", story.Number, issue.Key);

            //TODO : late bind? maybe??
            if (issue.Fields.Status != null && issue.Fields.Status.Name == "Done" && story.AssetState != "128")
            {
                await _v1.CloseStory(story.ID);
                _log.DebugFormat("Closed story {0}", story.Number);
            }
        }

        public void CreateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Creating V1 stories...");
            var processedStories = 0;
            var newStories = allJiraStories.Where(jStory =>
            {
                if (allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vStory => !string.IsNullOrWhiteSpace(vStory.Reference) &&
                                                              vStory.Reference.Contains(jStory.Key)) == null;
            }).ToList();

            _log.DebugFormat("Found {0} stories to check for create", newStories.Count);

            newStories.ForEach(async newJStory =>
            {
                await CreateStoryFromJira(jiraInfo, newJStory);
                processedStories++;
            });

            _log.InfoFormat("Created {0} stories", processedStories);
            _log.Trace("Creating V1 stories stopped...");
        }

        public async Task CreateStoryFromJira(V1JiraInfo jiraInfo, Issue jiraStory)
        {
            _log.TraceFormat("Attempting to create story from Jira story {0}", jiraStory.Key);
            var story = jiraStory.ToV1Story(jiraInfo.V1ProjectId);

            if (!string.IsNullOrEmpty(jiraStory.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraStory.Fields.EpicLink);
                story.Super = epicId;
            }

            var newStory = await _v1.CreateStory(story);
            _log.DebugFormat("Created {0} from Jira story {1}", newStory.Number, jiraStory.Key);

            await _v1.RefreshBasicInfo(newStory);

            jiraInfo.JiraInstance.UpdateIssue(newStory.ToIssueWithOnlyNumberAsLabel(jiraStory.Fields.Labels), jiraStory.Key);
            _log.TraceFormat("Updated labels on Jira story {0}", jiraStory.Key);

            jiraInfo.JiraInstance.AddLinkToV1InComments(jiraStory.Key, newStory.Number, newStory.ProjectName,
                _v1.InstanceUrl);
            _log.TraceFormat("Added link to V1 story {0} on Jira story {1}", newStory.Number, jiraStory.Key);
        }

        public void DeleteV1Stories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            _log.Trace("Deleting V1 stories...");
            var processedStories = 0;
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Story => !v1Story.IsInactive && !string.IsNullOrWhiteSpace(v1Story.Reference))
                    .Select(v1Story => v1Story.Reference);
            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraStoryKey => !allJiraStories.Any(js => js.Key.Equals(jiraStoryKey))).ToList();

            _log.DebugFormat("Found {0} stories to check for delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                _log.TraceFormat("Attempting to delete V1 story referencing jira story {0}", key);
                _v1.DeleteStoryWithJiraReference(jiraInfo.V1ProjectId, key);
                _log.DebugFormat("Deleted V1 story referencing jira story {0}", key);
                processedStories++;
            });

            _log.InfoFormat("Deleted {0} stories", processedStories);
            _log.Trace("Delete V1 stories stopped...");
        }
        #endregion STORIES

        #region DEFECTS
        public async Task DoDefectWork(V1JiraInfo jiraInfo)
        {
            _log.Trace("Defect sync started...");
            var allJiraDefects = jiraInfo.JiraInstance.GetDefectsInProject(jiraInfo.JiraKey).issues;
            var allV1Defects = await _v1.GetDefectsWithJiraReference(jiraInfo.V1ProjectId);

            UpdateDefects(jiraInfo, allJiraDefects, allV1Defects);
            CreateDefects(jiraInfo, allJiraDefects, allV1Defects);
            DeleteV1Defects(jiraInfo, allJiraDefects, allV1Defects);
            _log.Trace("Defect sync stopped...");
        }

        public void UpdateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraDefects, List<Defect> allV1Defects)
        {
            _log.Trace("Updating V1 defects...");
            var processedDefects = 0;
            var existingDefects =
                allJiraDefects.Where(jDefect => { return allV1Defects.Any(x => jDefect.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            _log.DebugFormat("Found {0} defects to check for update", existingDefects.Count);

            existingDefects.ForEach(async existingJDefect =>
            {
                var defect = allV1Defects.Single(x => existingJDefect.Fields.Labels.Contains(x.Number));
                if (existingJDefect.ItMatchesDefect(defect))
                    return;

                await UpdateDefectFromJiraToV1(jiraInfo, existingJDefect, defect);
                processedDefects++;
            });

            _log.InfoFormat("Updated {0} defects", processedDefects);
            _log.Trace("Updating V1 defects stopped...");
        }

        public async Task UpdateDefectFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Defect defect)
        {
            //need to reopen a Defect first before we can update it
            if (issue.Fields.Status != null && issue.Fields.Status.Name != "Done" && defect.AssetState == "128")
            {
                await _v1.ReOpenDefect(defect.ID);
                _log.TraceFormat("Reopened {0}", defect.Number);
            }

            var update = issue.ToV1Defect(jiraInfo.V1ProjectId);
            update.ID = defect.ID;

            _log.TraceFormat("Attempting to update {0}", defect.Number);
            await _v1.UpdateAsset(update, update.CreateUpdatePayload());

            _log.DebugFormat("Updated defect {0}", defect.Number);

            //TODO : late bind? maybe??
            if (issue.Fields.Status != null && issue.Fields.Status.Name == "Done" && defect.AssetState != "128")
            {
                await _v1.CloseDefect(defect.ID);
                _log.TraceFormat("Closed {0}", defect.Number);
            }
        }

        public void CreateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            _log.Trace("Creating V1 defects...");
            var processedDefects = 0;
            var newStories = allJiraStories.Where(jDefect =>
            {
                if (allV1Stories.Any(x => jDefect.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vDefect => !string.IsNullOrWhiteSpace(vDefect.Reference) &&
                                                              vDefect.Reference.Contains(jDefect.Key)) == null;
            }).ToList();

            _log.DebugFormat("Found {0} defects to check for create", newStories.Count);

            newStories.ForEach(newJDefect =>
            {
                CreateDefectFromJira(jiraInfo, newJDefect);
                processedDefects++;
            });

            _log.InfoFormat("Created {0} defects", processedDefects);
            _log.Trace("Creating V1 defects stopped...");
        }

        public async Task CreateDefectFromJira(V1JiraInfo jiraInfo, Issue jiraDefect)
        {
            var defect = jiraDefect.ToV1Defect(jiraInfo.V1ProjectId);

            if (!string.IsNullOrEmpty(jiraDefect.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraDefect.Fields.EpicLink);
                defect.Super = epicId;
            }

            _log.TraceFormat("Attempting to create V1 defect from Jira defect {0}", jiraDefect.Key);
            var newDefect = await _v1.CreateDefect(defect);
            _log.DebugFormat("Created {0} from Jira defect {1}", newDefect.Number, jiraDefect.Key);

            await _v1.RefreshBasicInfo(newDefect);

            jiraInfo.JiraInstance.UpdateIssue(newDefect.ToIssueWithOnlyNumberAsLabel(jiraDefect.Fields.Labels), jiraDefect.Key);
            _log.TraceFormat("Updated labels on Jira defect {0}", jiraDefect.Key);
            jiraInfo.JiraInstance.AddLinkToV1InComments(jiraDefect.Key, newDefect.Number, newDefect.ProjectName, _v1.InstanceUrl);
            _log.TraceFormat("Added link to V1 defect {0} on Jira defect {1}", newDefect.Number, jiraDefect.Key);
        }

        public void DeleteV1Defects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            _log.Trace("Deleting defects...");
            var processedDefects = 0;
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Defect => !v1Defect.IsInactive && !string.IsNullOrWhiteSpace(v1Defect.Reference))
                    .Select(v1Defect => v1Defect.Reference);

            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraDefectKey => !allJiraStories.Any(js => js.Key.Equals(jiraDefectKey))).ToList();

            _log.DebugFormat("Found {0} defects to delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                _log.TraceFormat("Attempting to delete V1 defect referencing jira defect {0}", key);
                _v1.DeleteDefectWithJiraReference(jiraInfo.V1ProjectId, key);
                _log.DebugFormat("Deleted V1 defect referencing jira defect {0}", key);
                processedDefects++;
            });

            _log.InfoFormat("Deleted {0} defects", processedDefects);
            _log.Trace("Deleting defects stopped...");
        }
        #endregion DEFECTS
    }
}