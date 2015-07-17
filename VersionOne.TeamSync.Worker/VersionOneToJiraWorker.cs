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
        private static readonly ILog Log = LogManager.GetLogger(typeof(VersionOneToJiraWorker));
        private readonly IEnumerable<V1JiraInfo> _jiraInstances;
        private readonly IV1 _v1;
        private static DateTime _syncTime;
        private readonly string[] _doneWords = { "Done", "Closed" };

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
            Log.Info("Beginning sync...");
            _jiraInstances.ToList().ForEach(async jiraInfo =>
            {
                _syncTime = DateTime.Now;
                Log.Info(string.Format("Syncing between {0} and {1}", jiraInfo.JiraKey, jiraInfo.V1ProjectId));

                await DoEpicWork(jiraInfo);
                await DoStoryWork(jiraInfo); //this will be broken out to its own thing :-)
                await DoDefectWork(jiraInfo);
                jiraInfo.JiraInstance.CleanUpAfterRun(Log);
            });
            Log.Info("Ending sync...");
            Log.DebugFormat("Total sync time: {0}", DateTime.Now - _syncTime);
        }

        public void ValidateConnections()
        {
            Log.Info("Verifying VersionOne connection...");
            Log.DebugFormat("URL: {0}", _v1.InstanceUrl);
            if (_v1.ValidateConnection())
                Log.Info("VersionOne connection successful!");
            else
            {
                Log.Error("VersionOne connection failed.");
                throw new Exception(string.Format("Unable to validate connection to {0}.", _v1.InstanceUrl));
            }

            foreach (var jiraInstance in _jiraInstances.ToList())
            {
                Log.InfoFormat("Verifying Jira connection...");
                Log.DebugFormat("URL: {0}", jiraInstance.JiraInstance.InstanceUrl);
                Log.Info(jiraInstance.ValidateConnection() ? "Jira connection successful!" : "Jira connection failed!");
            }
        }

        public void ValidateProjectMappings()
        {
            foreach (var jiraInstance in _jiraInstances.ToList())
            {
                Log.InfoFormat("Verifying V1ProjectID={1} to JiraProjectID={0} project mapping...", jiraInstance.JiraKey, jiraInstance.V1ProjectId);

                if (jiraInstance.ValidateMapping(_v1))
                {
                    Log.Info("Mapping successful! Projects will be synchronized.");
                }
                else
                {
                    Log.Error("Mapping failed. Projects will not be synchronized.");
                    ((HashSet<V1JiraInfo>)_jiraInstances).Remove(jiraInstance);
                }
            }
        }

        public void ValidateRequiredV1Fields()
        {
            Log.Info("Verifying VersionOne required fields...");
            if (!_v1.ValidateActualReferenceFieldExists())
            {
                Log.Warn("Actual.Reference field is missing in VersionOne instance.");
                throw new Exception("Unable to validate required field Actual.Reference");
            }
        }

        #region EPICS
        public async Task DoEpicWork(V1JiraInfo jiraInfo)
        {
            Log.Trace("Epic sync started...");
            await CreateEpics(jiraInfo);
            await UpdateEpics(jiraInfo);
            await ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
            await DeleteEpics(jiraInfo);
            Log.Trace("Epic sync stopped...");
        }

        public async Task DeleteEpics(V1JiraInfo jiraInfo)
        {
            Log.Trace("Deleting epics started");
            var processedEpics = 0;
            var deletedEpics = await _v1.GetDeletedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            Log.DebugFormat("Found {0} epics to check for delete", deletedEpics.Count);

            deletedEpics.ForEach(epic =>
            {
                Log.TraceFormat("Attempting to delete Jira epic {0}", epic.Reference);

                jiraInfo.JiraInstance.DeleteEpicIfExists(epic.Reference);
                Log.DebugFormat("Deleted epic Jira epic {0}", epic.Reference);

                _v1.RemoveReferenceOnDeletedEpic(epic);
                Log.TraceFormat("Removed reference on V1 epic {0}", epic.Number);

                processedEpics++;
            });

            Log.InfoFormat("Deleted {0} Jira epics", processedEpics);
            Log.Trace("Delete epics stopped");
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(V1JiraInfo jiraInfo)
        {
            Log.Trace("Resolving epics started");
            var processedEpics = 0;
            var closedEpics = await _v1.GetClosedTrackedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            Log.TraceFormat("Found {0} epics to check for resolve", closedEpics.Count);

            closedEpics.ForEach(epic =>
            {
                Log.TraceFormat("Attempting to resolve Jira epic {0}", epic.Reference);
                var jiraEpic = jiraInfo.JiraInstance.GetEpicByKey(epic.Reference);
                if (jiraEpic.HasErrors)
                {
                    //???
                    Log.ErrorFormat("Jira epic {0} has errors", epic.Reference);
                    return;
                }
                jiraInfo.JiraInstance.SetIssueToResolved(epic.Reference);
                Log.DebugFormat("Resolved Jira epic {0}", epic.Reference);
                processedEpics++;
            });

            Log.InfoFormat("Resolved {0} Jira epics", processedEpics);
            Log.Trace("Resolve epics stopped");
        }

        public async Task UpdateEpics(V1JiraInfo jiraInfo)
        {
            Log.Trace("Updating epics started");
            var processedEpics = 0;
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
            var searchResult = jiraInfo.JiraInstance.GetEpicsInProject(jiraInfo.JiraKey);

            if (searchResult.HasErrors)
            {
                searchResult.ErrorMessages.ForEach(Log.Error);
                return;
            }
            var jiraEpics = searchResult.issues;

            Log.DebugFormat("Found {0} epics to check for update", assignedEpics.Count);

            if (assignedEpics.Count > 0)
                Log.Trace("Recently updated epics : " + string.Join(", ", assignedEpics.Select(epic => epic.Number)));

            assignedEpics.ForEach(epic =>
            {
                Log.TraceFormat("Attempting to update Jira epic {0}", epic.Reference);
                var relatedJiraEpic = jiraEpics.FirstOrDefault(issue => issue.Key == epic.Reference);
                if (relatedJiraEpic == null)
                {
                    Log.Error("No related issue found in Jira for " + epic.Reference);
                    return;
                }

                if (relatedJiraEpic.Fields.Status.Name == "Done" && !epic.IsClosed())
                {
                    jiraInfo.JiraInstance.SetIssueToToDo(relatedJiraEpic.Key);
                    Log.DebugFormat("Set Jira epic {0} to ToDo", relatedJiraEpic.Key);
                }

                if (!epic.ItMatches(relatedJiraEpic))
                {
                    jiraInfo.JiraInstance.UpdateIssue(epic.UpdateJiraEpic(), relatedJiraEpic.Key);
                    Log.DebugFormat("Updated Jira epic {0} with data from V1 epic {1}", relatedJiraEpic.Key, epic.Number);
                }

                processedEpics++;
            });

            Log.InfoFormat("Updated {0} Jira epics", processedEpics);
            Log.Trace("Updating epics stopped");
        }

        public async Task CreateEpics(V1JiraInfo jiraInfo)
        {
            Log.Trace("Creating epics started");
            var processedEpics = 0;
            var unassignedEpics = await _v1.GetEpicsWithoutReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            Log.DebugFormat("Found {0} epics to check for create", unassignedEpics.Count);

            unassignedEpics.ForEach(epic =>
            {
                Log.TraceFormat("Attempting to create Jira epic from {0}", epic.Number);
                var jiraData = jiraInfo.JiraInstance.CreateEpic(epic, jiraInfo.JiraKey);

                Log.DebugFormat("Created Jira epic {0} from V1 epic {1}", jiraData.Key, epic.Number);

                if (jiraData.IsEmpty)
                {
                    Log.ErrorFormat(
                        "Saving epic failed. Possible reasons : Jira project ({0}) doesn't have epic type or expected custom field",
                        jiraInfo.JiraKey);
                }
                else
                {
                    jiraInfo.JiraInstance.AddCreatedByV1Comment(jiraData.Key, epic.Number, epic.ScopeName, _v1.InstanceUrl);
                    Log.TraceFormat("Added comment to Jira epic {0}", jiraData.Key);
                    epic.Reference = jiraData.Key;
                    _v1.UpdateEpicReference(epic);
                    Log.TraceFormat("Added reference in V1 epic {0}", epic.Number);
                    var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraData.Key;
                    _v1.CreateLink(epic, string.Format("Jira {0}", jiraData.Key), link);
                    Log.TraceFormat("Added link in V1 epic {0}", epic.Number);
                    processedEpics++;
                }
            });

            Log.InfoFormat("Created {0} Jira epics", processedEpics);
            Log.Trace("Create epics stopped");
        }
        #endregion EPICS

        #region STORIES
        public async Task DoStoryWork(V1JiraInfo jiraInfo)
        {
            Log.Trace("Story sync started...");
            var allJiraStories = jiraInfo.JiraInstance.GetStoriesInProject(jiraInfo.JiraKey).issues;
            var allV1Stories = await _v1.GetStoriesWithJiraReference(jiraInfo.V1ProjectId);

            UpdateStories(jiraInfo, allJiraStories, allV1Stories);
            CreateStories(jiraInfo, allJiraStories, allV1Stories);
            DeleteV1Stories(jiraInfo, allJiraStories, allV1Stories);

            DoActualWork(jiraInfo, allJiraStories, allV1Stories);
            Log.Trace("Story sync stopped...");
        }

        public async void UpdateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            Log.Trace("Updating stories started");
            var processedStories = 0;
            var existingStories =
                allJiraStories.Where(jStory => { return allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            Log.DebugFormat("Found {0} stories to check for update", existingStories.Count);

            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            existingStories.ForEach(existingJStory =>
            {
                var story = allV1Stories.Single(x => existingJStory.Fields.Labels.Contains(x.Number));

                UpdateStoryFromJiraToV1(jiraInfo, existingJStory, story, assignedEpics).Wait();
                processedStories++;
            });

            Log.InfoFormat("Finished checking {0} V1 stories", processedStories);
            Log.Trace("Updating stories stopped");
        }

        public async Task UpdateStoryFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Story story, List<Epic> assignedEpics)
        {
            Log.TraceFormat("Attempting to update V1 story {0}", story.Number);

            //need to reopen a story first before we can update it
            if (issue.Fields.Status != null && !issue.Fields.Status.Name.Is(_doneWords) && story.AssetState == "128")
            {
                await _v1.ReOpenStory(story.ID);
                Log.DebugFormat("Reopened story V1 {0}", story.Number);
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;
            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;
            var update = issue.ToV1Story(jiraInfo.V1ProjectId);
            update.ID = story.ID;

            if (!issue.ItMatchesStory(story))
            {
                update.Super = v1EpicId;
                await _v1.UpdateAsset(update, update.CreateUpdatePayload());
                Log.DebugFormat("Updated story V1 {0}", story.Number);
            }

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(_doneWords) && story.AssetState != "128")
            {
                await _v1.CloseStory(story.ID);
                Log.DebugFormat("Closed V1 story {0}", story.Number);
            }
        }

        public void CreateStories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            Log.Trace("Creating stories started");
            var processedStories = 0;
            var newStories = allJiraStories.Where(jStory =>
            {
                if (allV1Stories.Any(x => jStory.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vStory => !string.IsNullOrWhiteSpace(vStory.Reference) &&
                                                              vStory.Reference.Contains(jStory.Key)) == null;
            }).ToList();

            Log.DebugFormat("Found {0} stories to check for create", newStories.Count);

            newStories.ForEach(async newJStory =>
            {
                await CreateStoryFromJira(jiraInfo, newJStory);
                processedStories++;
            });

            Log.InfoFormat("Created {0} V1 stories", processedStories);
            Log.Trace("Creating stories stopped");
        }

        public async Task CreateStoryFromJira(V1JiraInfo jiraInfo, Issue jiraStory)
        {
            Log.TraceFormat("Attempting to create story from Jira story {0}", jiraStory.Key);
            var story = jiraStory.ToV1Story(jiraInfo.V1ProjectId);

            if (!string.IsNullOrEmpty(jiraStory.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraStory.Fields.EpicLink);
                story.Super = epicId;
            }

            var newStory = await _v1.CreateStory(story);
            Log.DebugFormat("Created {0} from Jira story {1}", newStory.Number, jiraStory.Key);

            await _v1.RefreshBasicInfo(newStory);

            jiraInfo.JiraInstance.UpdateIssue(newStory.ToIssueWithOnlyNumberAsLabel(jiraStory.Fields.Labels), jiraStory.Key);
            Log.TraceFormat("Updated labels on Jira story {0}", jiraStory.Key);

            jiraInfo.JiraInstance.AddLinkToV1InComments(jiraStory.Key, newStory.Number, newStory.ScopeName,
                _v1.InstanceUrl);
            Log.TraceFormat("Added link to V1 story {0} on Jira story {1}", newStory.Number, jiraStory.Key);

            var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraStory.Key;
            _v1.CreateLink(newStory, string.Format("Jira {0}", jiraStory.Key), link);
            Log.TraceFormat("Added link in V1 story {0}", newStory.Number);
        }

        public void DeleteV1Stories(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Story> allV1Stories)
        {
            Log.Trace("Deleting stories started");
            var processedStories = 0;
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Story => !v1Story.IsInactive && !string.IsNullOrWhiteSpace(v1Story.Reference))
                    .Select(v1Story => v1Story.Reference);
            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraStoryKey => !allJiraStories.Any(js => js.Key.Equals(jiraStoryKey))).ToList();

            Log.DebugFormat("Found {0} stories to check for delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                Log.TraceFormat("Attempting to delete V1 story referencing jira story {0}", key);
                _v1.DeleteStoryWithJiraReference(jiraInfo.V1ProjectId, key);
                Log.DebugFormat("Deleted V1 story referencing jira story {0}", key);
                processedStories++;
            });

            Log.InfoFormat("Deleted {0} V1 stories", processedStories);
            Log.Trace("Delete stories stopped");
        }
        #endregion STORIES

        #region DEFECTS
        public async Task DoDefectWork(V1JiraInfo jiraInfo)
        {
            Log.Trace("Defect sync started...");
            var allJiraDefects = jiraInfo.JiraInstance.GetDefectsInProject(jiraInfo.JiraKey).issues;
            var allV1Defects = await _v1.GetDefectsWithJiraReference(jiraInfo.V1ProjectId);

            UpdateDefects(jiraInfo, allJiraDefects, allV1Defects);
            CreateDefects(jiraInfo, allJiraDefects, allV1Defects);
            DeleteV1Defects(jiraInfo, allJiraDefects, allV1Defects);

            DoActualWork(jiraInfo, allJiraDefects, allV1Defects);
            Log.Trace("Defect sync stopped...");
        }

        public async void UpdateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraDefects, List<Defect> allV1Defects)
        {
            Log.Trace("Updating defects started");
            var processedDefects = 0;
            var existingDefects =
                allJiraDefects.Where(jDefect => { return allV1Defects.Any(x => jDefect.Fields.Labels.Contains(x.Number)); })
                    .ToList();

            Log.DebugFormat("Found {0} defects to check for update", existingDefects.Count);
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            existingDefects.ForEach(existingJDefect =>
            {
                var defect = allV1Defects.Single(x => existingJDefect.Fields.Labels.Contains(x.Number));

                UpdateDefectFromJiraToV1(jiraInfo, existingJDefect, defect, assignedEpics).Wait();
                processedDefects++;
            });

            Log.InfoFormat("Finished processing {0} V1 defects", processedDefects);
            Log.Trace("Updating defects stopped");
        }

        public async Task UpdateDefectFromJiraToV1(V1JiraInfo jiraInfo, Issue issue, Defect defect, List<Epic> assignedEpics)
        {
            //need to reopen a Defect first before we can update it
            if (issue.Fields.Status != null && !issue.Fields.Status.Name.Is(_doneWords) && defect.AssetState == "128")
            {
                await _v1.ReOpenDefect(defect.ID);
                Log.TraceFormat("Reopened V1 defect {0}", defect.Number);
            }

            var currentAssignedEpic = assignedEpics.FirstOrDefault(epic => epic.Reference == issue.Fields.EpicLink);
            var v1EpicId = currentAssignedEpic == null ? "" : "Epic:" + currentAssignedEpic.ID;

            if (currentAssignedEpic != null)
                issue.Fields.EpicLink = currentAssignedEpic.Number;

            var update = issue.ToV1Defect(jiraInfo.V1ProjectId);
            update.ID = defect.ID;

            if (!issue.ItMatchesDefect(defect))
            {
                update.Super = v1EpicId;
                Log.TraceFormat("Attempting to update V1 defect {0}", defect.Number);
                await _v1.UpdateAsset(update, update.CreateUpdatePayload());
            }

            Log.DebugFormat("Updated V1 defect {0}", defect.Number);

            if (issue.Fields.Status != null && issue.Fields.Status.Name.Is(_doneWords) && defect.AssetState != "128")
            {
                await _v1.CloseDefect(defect.ID);
                Log.TraceFormat("Closed V1 defect {0}", defect.Number);
            }
        }

        public void CreateDefects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            Log.Trace("Creating defects started");
            var processedDefects = 0;
            var newStories = allJiraStories.Where(jDefect =>
            {
                if (allV1Stories.Any(x => jDefect.Fields.Labels.Contains(x.Number)))
                    return false;

                return allV1Stories.SingleOrDefault(vDefect => !string.IsNullOrWhiteSpace(vDefect.Reference) &&
                                                              vDefect.Reference.Contains(jDefect.Key)) == null;
            }).ToList();

            Log.DebugFormat("Found {0} defects to check for create", newStories.Count);

            newStories.ForEach(async newJDefect =>
            {
                await CreateDefectFromJira(jiraInfo, newJDefect);
                processedDefects++;
            });

            Log.TraceFormat("Created {0} V1 defects", processedDefects);
            Log.Trace("Creating defects stopped");
        }

        public async Task CreateDefectFromJira(V1JiraInfo jiraInfo, Issue jiraDefect)
        {
            var defect = jiraDefect.ToV1Defect(jiraInfo.V1ProjectId);

            if (!string.IsNullOrEmpty(jiraDefect.Fields.EpicLink))
            {
                var epicId = await _v1.GetAssetIdFromJiraReferenceNumber("Epic", jiraDefect.Fields.EpicLink);
                defect.Super = epicId;
            }

            Log.TraceFormat("Attempting to create V1 defect from Jira defect {0}", jiraDefect.Key);
            var newDefect = await _v1.CreateDefect(defect);
            Log.DebugFormat("Created {0} from Jira defect {1}", newDefect.Number, jiraDefect.Key);

            await _v1.RefreshBasicInfo(newDefect);

            jiraInfo.JiraInstance.UpdateIssue(newDefect.ToIssueWithOnlyNumberAsLabel(jiraDefect.Fields.Labels), jiraDefect.Key);
            Log.TraceFormat("Updated labels on Jira defect {0}", jiraDefect.Key);
            jiraInfo.JiraInstance.AddLinkToV1InComments(jiraDefect.Key, newDefect.Number, newDefect.ScopeName, _v1.InstanceUrl);
            Log.TraceFormat("Added link to V1 defect {0} on Jira defect {1}", newDefect.Number, jiraDefect.Key);

            var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraDefect.Key;
            _v1.CreateLink(newDefect, string.Format("Jira {0}", jiraDefect.Key), link);
            Log.TraceFormat("Added link in V1 defect {0}", newDefect.Number);
        }

        public void DeleteV1Defects(V1JiraInfo jiraInfo, List<Issue> allJiraStories, List<Defect> allV1Stories)
        {
            Log.Trace("Deleting defects started");
            var processedDefects = 0;
            var jiraReferencedStoriesKeys =
                allV1Stories.Where(v1Defect => !v1Defect.IsInactive && !string.IsNullOrWhiteSpace(v1Defect.Reference))
                    .Select(v1Defect => v1Defect.Reference);

            var jiraDeletedStoriesKeys =
                jiraReferencedStoriesKeys.Where(jiraDefectKey => !allJiraStories.Any(js => js.Key.Equals(jiraDefectKey))).ToList();

            Log.DebugFormat("Found {0} defects to delete", jiraDeletedStoriesKeys.Count);

            jiraDeletedStoriesKeys.ForEach(key =>
            {
                Log.TraceFormat("Attempting to delete V1 defect referencing jira defect {0}", key);
                _v1.DeleteDefectWithJiraReference(jiraInfo.V1ProjectId, key);
                Log.DebugFormat("Deleted V1 defect referencing jira defect {0}", key);
                processedDefects++;
            });

            Log.InfoFormat("Deleted {0} V1 defects", processedDefects);
            Log.Trace("Deleting defects stopped");
        }
        #endregion DEFECTS

        #region ACTUALS
        private void DoActualWork<T>(V1JiraInfo jiraInfo, List<Issue> allJiraIssues, List<T> allV1WorkItems) where T : IPrimaryWorkItem
        {
            foreach (var issueKey in allJiraIssues.Select(issue => issue.Key))
            {
                var workItem = allV1WorkItems.FirstOrDefault(s => s.Reference.Equals(issueKey));
                if (workItem != null && !workItem.Equals(default(T)))
                {
                    var workItemId = string.Format("{0}:{1}", workItem.GetType().Name, workItem.ID);

                    Log.TraceFormat("Getting Jira Worklogs for Issue key: {0}", issueKey);
                    var worklogs = jiraInfo.JiraInstance.GetIssueWorkLogs(issueKey).ToList();

                    Log.TraceFormat("Getting V1 actuals for Workitem Oid: {0}", workItemId);
                    var actuals = _v1.GetWorkItemActuals(jiraInfo.V1ProjectId, workItemId).Result.ToList();

                    Log.Trace("Creating actuals started");
                    var newWorklogs = worklogs.Where(w => !actuals.Any(a => a.Reference.Equals(w.id.ToString()))).ToList();
                    if (newWorklogs.Any())
                        CreateActualsFromWorklogs(jiraInfo, newWorklogs, workItemId, workItem.Number, issueKey);
                    Log.Trace("Creating actuals stopped");

                    Log.Trace("Updating actual started");
                    var updateWorklogs = worklogs.Where(w => actuals.Any(a => a.Reference.Equals(w.id.ToString()) &&
                        // Have started date changed?
                        (!w.started.ToString(CultureInfo.InvariantCulture).Equals(a.Date.ToString(CultureInfo.InvariantCulture)) ||
                        // Have worked hours changed?
                        double.Parse(a.Value).CompareTo(w.timeSpentSeconds / 3600d) != 0))).ToList();
                    if (updateWorklogs.Any())
                        UpdateActualsFromWorklogs(jiraInfo, updateWorklogs, workItemId, actuals);
                    Log.Trace("Updating actual stopped");

                    Log.Trace("Deleting actuals started");
                    var actualsToDelete = actuals.Where(a => !worklogs.Any(w => w.id.ToString().Equals(a.Reference)) &&
                        !a.Value.Equals("0")).ToList();
                    if (actualsToDelete.Any())
                        DeleteActualsFromWorklogs(actualsToDelete);
                    Log.Trace("Deleting actuals stopped");
                }
            }
        }

        private void CreateActualsFromWorklogs(V1JiraInfo jiraInfo, List<Worklog> newWorklogs, string workItemId, string v1Number, string issueKey)
        {
            Log.DebugFormat("Found {0} worklogs to check for create", newWorklogs.Count());
            var processedActuals = 0;
            foreach (var worklog in newWorklogs)
            {
                Log.TraceFormat("Attempting to create actual from Jira worklog id {0}", worklog.id);
                var actual = worklog.ToV1Actual(_v1.MemberId, jiraInfo.V1ProjectId, workItemId);
                var newActual = _v1.CreateActual(actual).Result;
                Log.DebugFormat("Created V1 actual id {0} from Jira worklog id {1}", newActual.ID, worklog.id);

                var actualOid = string.Format("{0}:{1}", newActual.AssetType, newActual.ID);
                jiraInfo.JiraInstance.AddCreatedAsVersionOneActualComment(issueKey, actualOid, v1Number);
                Log.TraceFormat("Added comment on Jira worklog id {0} with new V1 actual id {1}", worklog.id, newActual.ID);

                processedActuals++;
            }
            Log.InfoFormat("Created {0} V1 actuals", processedActuals);
        }

        private void UpdateActualsFromWorklogs(V1JiraInfo jiraInfo, List<Worklog> updateWorklogs, string workItemId, List<Actual> actuals)
        {
            Log.DebugFormat("Found {0} worklogs to check for update", updateWorklogs.Count());
            var processedActuals = 0;
            foreach (var worklog in updateWorklogs)
            {
                var actual = worklog.ToV1Actual(_v1.MemberId, jiraInfo.V1ProjectId, workItemId);
                actual.ID = actuals.Single(a => a.Reference.Equals(worklog.id.ToString())).ID;

                Log.TraceFormat("Attempting to update actual id {0} from Jira worklog id {1}", actual.ID, worklog.id);
                _v1.UpdateAsset(actual, actual.CreatePayload());
                Log.DebugFormat("Updated V1 actual id {0}", actual.ID);

                processedActuals++;
            }
            Log.InfoFormat("Updated {0} V1 actuals", processedActuals);
        }

        private void DeleteActualsFromWorklogs(List<Actual> actualsToDelete)
        {
            Log.DebugFormat("Found {0} actuals to check for delete", actualsToDelete.Count);
            var processedActuals = 0;
            foreach (var actual in actualsToDelete)
            {
                Log.TraceFormat("Attempting to update actual id {0} with value 0", actual.ID);
                actual.Value = "0";
                _v1.UpdateAsset(actual, actual.CreatePayload());
                Log.DebugFormat("Updated V1 actual id {0}", actual.ID);

                processedActuals++;
            }
            Log.InfoFormat("Deleted {0} V1 actuals", processedActuals);
        }
        #endregion ACTUALS
    }
}
