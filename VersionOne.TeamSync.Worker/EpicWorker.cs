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
            await CreateEpics(jiraInfo);
            await UpdateEpics(jiraInfo);
            await ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
            await DeleteEpics(jiraInfo);
            _log.Trace("Epic sync stopped...");
        }

        public async Task DeleteEpics(V1JiraInfo jiraInfo)
        {
            _log.Trace("Deleting epics started");
            var processedEpics = 0;
            var deletedEpics = await _v1.GetDeletedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            _log.DebugFormat("Found {0} epics to check for delete", deletedEpics.Count);

            deletedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to delete Jira epic {0}", epic.Reference);

                jiraInfo.JiraInstance.DeleteEpicIfExists(epic.Reference);
                _log.DebugFormat("Deleted epic Jira epic {0}", epic.Reference);

                _v1.RemoveReferenceOnDeletedEpic(epic);
                _log.TraceFormat("Removed reference on V1 epic {0}", epic.Number);

                processedEpics++;
            });

            if (processedEpics > 0) _log.InfoFormat("Deleted {0} Jira epics", processedEpics);
            _log.Trace("Delete epics stopped");
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(V1JiraInfo jiraInfo)
        {
            _log.Trace("Resolving epics started");
            var processedEpics = 0;
            var closedEpics = await _v1.GetClosedTrackedEpics(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);

            _log.TraceFormat("Found {0} epics to check for resolve", closedEpics.Count);

            closedEpics.ForEach(epic =>
            {
                var jiraEpic = jiraInfo.JiraInstance.GetEpicByKey(epic.Reference);

                if (jiraEpic.HasErrors)
                {
                    _log.ErrorFormat("Jira epic {0} has errors", epic.Reference);
                    return;
                }

                if (jiraInfo.DoneWords.Contains(jiraEpic.issues.Single().Fields.Status.Name))
                    return;

                _log.TraceFormat("Attempting to resolve Jira epic {0}", epic.Reference);

                jiraInfo.JiraInstance.SetIssueToResolved(epic.Reference, jiraInfo.DoneWords);
                _log.DebugFormat("Resolved Jira epic {0}", epic.Reference);
                processedEpics++;
            });

           if (processedEpics > 0)  _log.InfoFormat("Resolved {0} Jira epics", processedEpics);
            _log.Trace("Resolve epics stopped");
        }

        public async Task UpdateEpics(V1JiraInfo jiraInfo)
        {
            //bool updatedEpics = false;
            _log.Trace("Updating epics started");
            var updatedEpics = 0;
           // var processedEpics = 0;
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInfo.V1ProjectId, jiraInfo.EpicCategory);
            var searchResult = jiraInfo.JiraInstance.GetEpicsInProject(jiraInfo.JiraKey);

            if (searchResult.HasErrors)
            {
                searchResult.ErrorMessages.ForEach(_log.Error);
                return;
            }
            var jiraEpics = searchResult.issues;

            _log.DebugFormat("Found {0} epics to check for update", assignedEpics.Count);

            if (assignedEpics.Count > 0)
                _log.Trace("Recently updated epics : " + string.Join(", ", assignedEpics.Select(epic => epic.Number)));

            assignedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to update Jira epic {0}", epic.Reference);
                var relatedJiraEpic = jiraEpics.FirstOrDefault(issue => issue.Key == epic.Reference);
                if (relatedJiraEpic == null)
                {
                    _log.Error("No related issue found in Jira for " + epic.Reference);
                    return;
                }

                if (jiraInfo.DoneWords.Contains(relatedJiraEpic.Fields.Status.Name) && !epic.IsClosed())
                {
                    jiraInfo.JiraInstance.SetIssueToToDo(relatedJiraEpic.Key, jiraInfo.DoneWords);
                    _log.DebugFormat("Set Jira epic {0} to ToDo", relatedJiraEpic.Key);
                }

                if (!epic.ItMatches(relatedJiraEpic))
                {
                    jiraInfo.JiraInstance.UpdateIssue(epic.UpdateJiraEpic(relatedJiraEpic.Fields.Labels), relatedJiraEpic.Key);
                    _log.DebugFormat("Updated Jira epic {0} with data from V1 epic {1}", relatedJiraEpic.Key, epic.Number);
                    updatedEpics++;
                }

               // processedEpics++;
            });

            //_log.InfoFormat("Finished checking {0} V1 Epics", processedEpics);
            if (updatedEpics > 0) _log.InfoUpdated(updatedEpics, PluralAsset);
            _log.Trace("Updating epics stopped");
        }

        public async Task CreateEpics(V1JiraInfo jiraInfo)
        {
            _log.Trace("Creating epics started");
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
                    jiraInfo.JiraInstance.AddComment(jiraData.Key, string.Format(CreatedFromV1Comment, epic.Number, epic.ScopeName));
                    _log.TraceFormat("Added comment to Jira epic {0}", jiraData.Key);
                    jiraInfo.JiraInstance.AddWebLink(jiraData.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, epic.Number),
                        string.Format(V1AssetDetailWebLinkTitle, epic.Number));
                    _log.TraceFormat("Added web link to Jira epic {0}", jiraData.Key);
                    epic.Reference = jiraData.Key;
                    _v1.UpdateEpicReference(epic);
                    _log.TraceFormat("Added reference in V1 epic {0}", epic.Number);
                    var link = jiraInfo.JiraInstance.InstanceUrl + "/browse/" + jiraData.Key;
                    _v1.CreateLink(epic, string.Format("Jira {0}", jiraData.Key), link);
                    _log.TraceFormat("Added link in V1 epic {0}", epic.Number);
                    processedEpics++;
                }
            });

            if (processedEpics > 0) _log.InfoFormat("Created {0} Jira epics", processedEpics);
            _log.Trace("Create epics stopped");
        }
    }
}
