using System;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker
{
    public class EpicWorker : IAsyncWorker
    {
        private const string PluralAsset = "epics";
        private const string CreatedFromV1Comment = "Created from VersionOne Portfolio Item {0} in Project {1}";
        private const string V1AssetDetailWebLinkUrl = "{0}assetdetail.v1?Number={1}";
        private const string V1AssetDetailWebLinkTitle = "VersionOne Portfolio Item ({0})";

        private readonly IV1 _v1;
        private readonly ILog _log;

        public EpicWorker(IV1 v1, ILog log)
        {
            _v1 = v1;
            _log = log;
        }

        public async Task DoWork(V1JiraInfo jiraInfo)
        {
            _log.Trace("Epic sync started...");
            await UpdateEpics(jiraInfo);
            await CreateEpics(jiraInfo);
            await ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
            await DeleteEpics(jiraInfo);
            _log.Trace("Epic sync stopped...");
        }

        public async Task CreateEpics(V1JiraInfo jiraInfo)
        {
            _log.Trace("Creating epics started");
            var processedEpics = 0;
            var unassignedV1Epics = await _v1.GetEpicsWithoutReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            if (unassignedV1Epics.Any())
                _log.DebugFormat("Found {0} epics to check for create", unassignedV1Epics.Count);

            unassignedV1Epics.ForEach(v1Epic =>
            {
                _log.TraceFormat("Attempting to create Jira epic from {0}", v1Epic.Number);
                var jiraData = jiraInfo.JiraInstance.CreateEpic(v1Epic, jiraInfo.JiraKey);

                _log.DebugFormat("Created Jira epic {0} from V1 epic {1}", jiraData.Key, v1Epic.Number);

                if (jiraData.IsEmpty)
                {
                    _log.ErrorFormat(
                        "Saving epic failed. Possible reasons : Jira project ({0}) doesn't have epic type or expected custom field",
                        jiraInfo.JiraKey);
                }
                else
                {
                    jiraInfo.JiraInstance.AddComment(jiraData.Key, string.Format(CreatedFromV1Comment, v1Epic.Number, v1Epic.ScopeName));
                    _log.TraceFormat("Added comment to Jira epic {0}", jiraData.Key);
                    jiraInfo.JiraInstance.AddWebLink(jiraData.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, v1Epic.Number),
                        string.Format(V1AssetDetailWebLinkTitle, v1Epic.Number));
                    _log.TraceFormat("Added web link to Jira epic {0}", jiraData.Key);
                    v1Epic.Reference = jiraData.Key;
                    _v1.UpdateEpicReference(v1Epic);
                    _log.TraceFormat("Added reference in V1 epic {0}", v1Epic.Number);
                    var link = new Uri(new Uri(jiraInfo.JiraInstance.InstanceUrl), string.Format("browse/{0}", jiraData.Key)).ToString();
                    _v1.CreateLink(v1Epic, string.Format("Jira {0}", jiraData.Key), link);
                    _log.TraceFormat("Added link in V1 epic {0}", v1Epic.Number);
                    processedEpics++;
                }
            });

           if (processedEpics > 0)  _log.DebugFormat("Resolved {0} Jira epics", processedEpics);
                _log.InfoFormat("Created {0} Jira epics", processedEpics);
            _log.Trace("Create epics stopped");
        }

        public async Task UpdateEpics(V1JiraInfo jiraInfo)
        {
            _log.Trace("Updating epics started");
            var processedEpics = 0;
            var reopenedEpics = 0;
            var assignedV1Epics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
           
            var searchResult = jiraInfo.JiraInstance.GetEpicsInProject(jiraInfo.JiraKey);

            if (searchResult.HasErrors)
            {
                searchResult.ErrorMessages.ForEach(_log.Error);
                return;
            }
            var jiraEpics = searchResult.issues;
            if (assignedV1Epics.Any())
            {
                _log.DebugFormat("Found {0} epics to check for update", assignedV1Epics.Count);
                _log.Trace("Recently updated epics : " + string.Join(", ", assignedV1Epics.Select(epic => epic.Number)));
            }
            
            assignedV1Epics.ForEach(v1Epic =>
            {
                _log.TraceFormat("Attempting to update Jira epic {0}", v1Epic.Reference);
                var relatedJiraEpic = jiraEpics.FirstOrDefault(issue => issue.Key == v1Epic.Reference);
                if (relatedJiraEpic == null)
                {
                    _log.Error("No related issue found in Jira for " + v1Epic.Reference);
                    return;
                }

                if (jiraInfo.DoneWords.Contains(relatedJiraEpic.Fields.Status.Name) && !v1Epic.IsClosed())
                {
                    jiraInfo.JiraInstance.SetIssueToToDo(relatedJiraEpic.Key, jiraInfo.DoneWords);
                    _log.DebugFormat("Set Jira epic {0} to ToDo", relatedJiraEpic.Key);
                    reopenedEpics++;
                }

                if (!v1Epic.ItMatches(relatedJiraEpic))
                {
                    jiraInfo.JiraInstance.UpdateIssue(v1Epic.UpdateJiraEpic(relatedJiraEpic.Fields.Labels), relatedJiraEpic.Key);
                    _log.DebugFormat("Updated Jira epic {0} with data from V1 epic {1}", relatedJiraEpic.Key, v1Epic.Number);
                    processedEpics++;
                }
               
            });

            if (processedEpics > 0)
            {
                _log.InfoUpdated(processedEpics, PluralAsset);
                _log.TraceFormat("Recently updated epics : {0}", string.Join(", ", assignedV1Epics.Select(epic => epic.Number)));
            }
            _log.Trace("Updating epics stopped");
        }

        public async Task DeleteEpics(V1JiraInfo jiraInfo)
        {
            _log.Trace("Deleting epics started");
            var processedEpics = 0;
            var deletedV1Epics = await _v1.GetDeletedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            if (deletedV1Epics.Any())
                _log.DebugFormat("Found {0} epics to check for delete", deletedV1Epics.Count);

            deletedV1Epics.ForEach(v1Epic =>
            {
                _log.TraceFormat("Attempting to delete Jira epic {0}", v1Epic.Reference);

                jiraInfo.JiraInstance.DeleteEpicIfExists(v1Epic.Reference);
                _log.DebugFormat("Deleted Jira epic {0}", v1Epic.Reference);

                _v1.RemoveReferenceOnDeletedEpic(v1Epic);
                _log.TraceFormat("Removed reference on V1 epic {0}", v1Epic.Number);

                processedEpics++;
            });

            if (processedEpics > 0)
                _log.InfoFormat("Deleted {0} Jira epics", processedEpics);
            _log.Trace("Delete epics stopped");
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(V1JiraInfo jiraInfo)
        {
            _log.Trace("Resolving epics started");
            var processedEpics = 0;
            var closedV1Epics = await _v1.GetClosedTrackedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            if (closedV1Epics.Any())
                _log.DebugFormat("Found {0} epics to check for resolve", closedV1Epics.Count);

            closedV1Epics.ForEach(v1Epic =>
            {
                var jiraEpic = jiraInfo.JiraInstance.GetEpicByKey(v1Epic.Reference);

                if (jiraEpic.HasErrors)
                {
                    _log.ErrorFormat("Jira epic {0} has errors", v1Epic.Reference);
                    return;
                }

                if (jiraInfo.DoneWords.Contains(jiraEpic.issues.Single().Fields.Status.Name))
                    return;

                _log.TraceFormat("Attempting to resolve Jira epic {0}", v1Epic.Reference);

                jiraInfo.JiraInstance.SetIssueToResolved(v1Epic.Reference, jiraInfo.DoneWords);
                _log.DebugFormat("Resolved Jira epic {0}", v1Epic.Reference);
                processedEpics++;
            });

            if (processedEpics > 0)
                _log.InfoFormat("Resolved {0} Jira epics", processedEpics);
            _log.Trace("Resolve epics stopped");
        }
    }
}