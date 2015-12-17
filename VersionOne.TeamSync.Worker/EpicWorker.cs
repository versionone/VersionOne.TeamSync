using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.JiraWorker.Extensions;
using VersionOne.TeamSync.VersionOne.Domain;

namespace VersionOne.TeamSync.JiraWorker
{
    public class EpicWorker : IAsyncWorker
    {
        private const string PluralAsset = "epics";
        private const string CreatedFromV1Comment = "Created from VersionOne Portfolio Item {0} in Project {1}";
        private const string V1AssetDetailWebLinkUrl = "{0}assetdetail.v1?Number={1}";
        private const string V1AssetDetailWebLinkTitle = "VersionOne Portfolio Item ({0})";

        private readonly IV1 _v1;
        private readonly ILog _log;
        private DateTime _lastSyncDate;

        public EpicWorker(IV1 v1, ILog log)
        {
            _v1 = v1;
            _log = log;
        }

        public async Task DoFirstRun(IJira jiraInstance)
        {
            _log.Trace("Epic sync started...");
            await CreateEpicsSince(jiraInstance);
            _log.Trace("Epic sync stopped...");
        }

        public async Task DoWork(IJira jiraInstance)
        {
            _lastSyncDate = DateTime.UtcNow.AddMinutes(-ServiceSettings.Settings.SyncIntervalInMinutes);
            _log.Trace("Epic sync started...");
            await CreateEpics(jiraInstance);
            await UpdateEpics(jiraInstance);
            await ClosedV1EpicsSetJiraEpicsToResolved(jiraInstance);
            await DeleteEpics(jiraInstance);
            _log.Trace("Epic sync stopped...");
        }

        public async Task CreateEpicsSince(IJira jiraInstance)
        {
            var epics = await _v1.GetEpicsWithoutReferenceCreatedSince(jiraInstance.V1Project, jiraInstance.EpicCategory, jiraInstance.RunFromThisDateOn);
            CreateEpics(jiraInstance, epics);
        }

        private void CreateEpics(IJira jiraInstance, List<Epic> epics)
        {
            _log.Trace("Creating epics started");
            var processedEpics = 0;

            var unassignedV1Epics = epics;

            if (unassignedV1Epics.Any())
                _log.DebugFormat("Found {0} epics to check for create", unassignedV1Epics.Count);

            unassignedV1Epics.ForEach(v1Epic =>
            {
                _log.TraceFormat("Attempting to create Jira epic from {0}", v1Epic.Number);

                var jiraData = jiraInstance.CreateEpic(v1Epic, jiraInstance.JiraProject);
                _log.DebugFormat("Created Jira epic {0} from V1 epic {1}", jiraData.Key, v1Epic.Number);

                if (jiraData.IsEmpty)
                {
                    _log.ErrorFormat(
                        "Saving epic failed. Possible reasons : Jira project ({0}) doesn't have epic type or expected custom field",
                        jiraInstance.JiraProject);
                }
                else
                {
                    if (!string.IsNullOrEmpty(v1Epic.Status))
                    {
                        var jiraStatusFromMapping = JiraSettings.GetInstance().GetJiraStatusFromMapping(jiraInstance.InstanceUrl, jiraInstance.JiraProject, v1Epic.Status);
                        if (jiraStatusFromMapping != null)
                        {
                            string transitionIdToRun = jiraInstance.GetIssueTransitionId(jiraData.Key, jiraStatusFromMapping);
                            if (transitionIdToRun != null)
                                jiraInstance.RunTransitionOnIssue(transitionIdToRun, jiraData.Key);
                        }
                    }

                    jiraInstance.AddComment(jiraData.Key, string.Format(CreatedFromV1Comment, v1Epic.Number, v1Epic.ScopeName));
                    _log.TraceFormat("Added comment to Jira epic {0}", jiraData.Key);
                    jiraInstance.AddWebLink(jiraData.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, v1Epic.Number),
                        string.Format(V1AssetDetailWebLinkTitle, v1Epic.Number));
                    _log.TraceFormat("Added web link to Jira epic {0}", jiraData.Key);
                    v1Epic.Reference = jiraData.Key;
                    _v1.UpdateEpicReference(v1Epic);
                    _log.TraceFormat("Added reference in V1 epic {0}", v1Epic.Number);
                    var link = new Uri(new Uri(jiraInstance.InstanceUrl), string.Format("browse/{0}", jiraData.Key)).ToString();
                    _v1.CreateLink(v1Epic, string.Format("Jira {0}", jiraData.Key), link);
                    _log.TraceFormat("Added link in V1 epic {0}", v1Epic.Number);
                    processedEpics++;
                }
            });

            if (processedEpics > 0)
                _log.InfoFormat("Created {0} Jira epics", processedEpics);
            _log.Trace("Create epics stopped");
        }

        public async Task CreateEpics(IJira jiraInstance)
        {
            var epics = await _v1.GetEpicsWithoutReferenceUpdatedSince(jiraInstance.V1Project, jiraInstance.EpicCategory, _lastSyncDate);
            // if CreateDateUTC == ChangeDateUTC then epic was not updated
            var updatedEpics = epics.Where(e => !e.CreateDateUTC.Equals(e.ChangeDateUTC)).ToList();

            CreateEpics(jiraInstance, updatedEpics);
        }

        public async Task UpdateEpics(IJira jiraInstance)
        {
            _log.Trace("Updating epics started");
            var processedEpics = 0;
            var reopenedEpics = 0;

            var searchResult = jiraInstance.GetEpicsInProject(jiraInstance.JiraProject);
            if (searchResult.HasErrors)
            {
                searchResult.ErrorMessages.ForEach(_log.Error);
                return;
            }

            var assignedV1Epics = await _v1.GetEpicsWithReferenceUpdatedSince(jiraInstance.V1Project, jiraInstance.EpicCategory, _lastSyncDate);

            if (assignedV1Epics.Any())
                _log.DebugFormat("Found {0} epics to check for update", assignedV1Epics.Count);

            var jiraEpics = searchResult.issues;
            assignedV1Epics.ForEach(v1Epic =>
            {
                _log.TraceFormat("Attempting to update Jira epic {0}", v1Epic.Reference);

                var relatedJiraEpic = jiraEpics.FirstOrDefault(issue => issue.Key == v1Epic.Reference);
                if (relatedJiraEpic == null)
                {
                    _log.Error("No related issue found in Jira for " + v1Epic.Reference);
                    return;
                }

                //if (jiraInstance.DoneWords.Contains(relatedJiraEpic.Fields.Status.Name) && !v1Epic.IsClosed())
                //{
                //    jiraInstance.SetIssueToToDo(relatedJiraEpic.Key, jiraInstance.DoneWords);
                //    _log.DebugFormat("Set Jira epic {0} to ToDo", relatedJiraEpic.Key);
                //    reopenedEpics++;
                //}

                var jiraStatusFromMapping = JiraSettings.GetInstance().GetJiraStatusFromMapping(jiraInstance.InstanceUrl, jiraInstance.JiraProject, v1Epic.Status);
                if (jiraStatusFromMapping != null && !relatedJiraEpic.Fields.Status.Name.Equals(jiraStatusFromMapping))
                {
                    var transitionIdToRun = jiraInstance.GetIssueTransitionId(relatedJiraEpic.Key, jiraStatusFromMapping);
                    if (transitionIdToRun != null)
                        jiraInstance.RunTransitionOnIssue(transitionIdToRun, relatedJiraEpic.Key);
                }

                var jiraPriorityIdFromMapping = jiraInstance.JiraSettings.GetJiraPriorityIdFromMapping(jiraInstance.InstanceUrl, v1Epic.Priority);
                if (!v1Epic.ItMatches(relatedJiraEpic) ||
                    (jiraPriorityIdFromMapping != relatedJiraEpic.Fields.Priority.Id))
                {
                    jiraInstance.UpdateIssue(v1Epic.UpdateJiraEpic(relatedJiraEpic.Fields.Labels, jiraPriorityIdFromMapping), relatedJiraEpic.Key);
                    _log.DebugFormat("Updated Jira epic {0} with data from V1 epic {1}", relatedJiraEpic.Key, v1Epic.Number);
                    processedEpics++;
                }
            });

            if (processedEpics > 0)
            {
                _log.InfoFormat("Updated {0} Jira epics", processedEpics);
                _log.TraceFormat("Recently updated epics : {0}", string.Join(", ", assignedV1Epics.Select(epic => epic.Number)));
            }
            _log.Trace("Updating epics stopped");
        }

        public async Task DeleteEpics(IJira jiraInstance)
        {
            _log.Trace("Deleting epics started");
            var processedEpics = 0;

            var deletedV1Epics = await _v1.GetDeletedEpicsUpdatedSince(jiraInstance.V1Project, jiraInstance.EpicCategory, _lastSyncDate);

            if (deletedV1Epics.Any())
                _log.DebugFormat("Found {0} epics to check for delete", deletedV1Epics.Count);

            deletedV1Epics.ForEach(v1Epic =>
            {
                _log.TraceFormat("Attempting to delete Jira epic {0}", v1Epic.Reference);

                jiraInstance.DeleteEpicIfExists(v1Epic.Reference);
                _log.DebugFormat("Deleted Jira epic {0}", v1Epic.Reference);

                _v1.RemoveReferenceOnDeletedEpic(v1Epic);
                _log.DebugFormat("Removed reference on V1 epic {0}", v1Epic.Number);

                processedEpics++;
            });

            if (processedEpics > 0)
                _log.InfoFormat("Deleted {0} Jira epics", processedEpics);
            _log.Trace("Delete epics stopped");
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(IJira jiraInstance)
        {
            _log.Trace("Resolving epics started");
            var processedEpics = 0;

            var closedV1Epics = await _v1.GetClosedTrackedEpicsUpdatedSince(jiraInstance.V1Project, jiraInstance.EpicCategory, _lastSyncDate);

            if (closedV1Epics.Any())
                _log.DebugFormat("Found {0} epics to check for resolve", closedV1Epics.Count);

            closedV1Epics.ForEach(v1Epic =>
            {
                var jiraEpic = jiraInstance.GetEpicByKey(v1Epic.Reference);

                if (jiraEpic.HasErrors)
                {
                    _log.ErrorFormat("Jira epic {0} has errors", v1Epic.Reference);
                    return;
                }

                if (jiraInstance.DoneWords.Contains(jiraEpic.issues.Single().Fields.Status.Name))
                    return;

                _log.TraceFormat("Attempting to resolve Jira epic {0}", v1Epic.Reference);

                jiraInstance.SetIssueToResolved(v1Epic.Reference, jiraInstance.DoneWords);
                _log.DebugFormat("Resolved Jira epic {0}", v1Epic.Reference);
                processedEpics++;
            });

            if (processedEpics > 0)
                _log.InfoFormat("Resolved {0} Jira epics", processedEpics);
            _log.Trace("Resolve epics stopped");
        }
    }
}