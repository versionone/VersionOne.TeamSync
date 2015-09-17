using System.Linq;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker
{
    public class EpicWorker : IAsyncWorker
    {
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

        public async Task DoWork(IJira jiraInstance)
        {
            _log.Trace("Epic sync started...");
            await CreateEpics(jiraInstance);
            await UpdateEpics(jiraInstance);
            await ClosedV1EpicsSetJiraEpicsToResolved(jiraInstance);
            await DeleteEpics(jiraInstance);
            _log.Trace("Epic sync stopped...");
        }

        public async Task DeleteEpics(IJira jiraInstance)
        {
            _log.Trace("Deleting epics started");
            var processedEpics = 0;
            var deletedEpics = await _v1.GetDeletedEpics(jiraInstance.V1Project, jiraInstance.EpicCategory);

            _log.DebugFormat("Found {0} epics to check for delete", deletedEpics.Count);

            deletedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to delete Jira epic {0}", epic.Reference);

                jiraInstance.DeleteEpicIfExists(epic.Reference);
                _log.DebugFormat("Deleted epic Jira epic {0}", epic.Reference);

                _v1.RemoveReferenceOnDeletedEpic(epic);
                _log.TraceFormat("Removed reference on V1 epic {0}", epic.Number);

                processedEpics++;
            });

            _log.InfoFormat("Deleted {0} Jira epics", processedEpics);
            _log.Trace("Delete epics stopped");
        }

        public async Task ClosedV1EpicsSetJiraEpicsToResolved(IJira jiraInstance)
        {
            _log.Trace("Resolving epics started");
            var processedEpics = 0;
            var closedEpics = await _v1.GetClosedTrackedEpics(jiraInstance.V1Project, jiraInstance.EpicCategory);

            _log.TraceFormat("Found {0} epics to check for resolve", closedEpics.Count);

            closedEpics.ForEach(epic =>
            {
                var jiraEpic = jiraInstance.GetEpicByKey(epic.Reference);

                if (jiraEpic.HasErrors)
                {
                    _log.ErrorFormat("Jira epic {0} has errors", epic.Reference);
                    return;
                }

                if (jiraInstance.DoneWords.Contains(jiraEpic.issues.Single().Fields.Status.Name))
                    return;

                _log.TraceFormat("Attempting to resolve Jira epic {0}", epic.Reference);

                jiraInstance.SetIssueToResolved(epic.Reference, jiraInstance.DoneWords);
                _log.DebugFormat("Resolved Jira epic {0}", epic.Reference);
                processedEpics++;
            });

            _log.InfoFormat("Resolved {0} Jira epics", processedEpics);
            _log.Trace("Resolve epics stopped");
        }

        public async Task UpdateEpics(IJira jiraInstance)
        {
            _log.Trace("Updating epics started");
            var processedEpics = 0;
            var assignedEpics = await _v1.GetEpicsWithReference(jiraInstance.V1Project, jiraInstance.EpicCategory);
            var searchResult = jiraInstance.GetEpicsInProject(jiraInstance.JiraProject);

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

                if (jiraInstance.DoneWords.Contains(relatedJiraEpic.Fields.Status.Name) && !epic.IsClosed())
                {
                    jiraInstance.SetIssueToToDo(relatedJiraEpic.Key, jiraInstance.DoneWords);
                    _log.DebugFormat("Set Jira epic {0} to ToDo", relatedJiraEpic.Key);
                }

                if (!epic.ItMatches(relatedJiraEpic) ||
                    (JiraSettings.GetPriorityIdFromMapping(jiraInstance.InstanceUrl, epic.Priority) != relatedJiraEpic.Fields.Priority.Id))
                {
                    jiraInstance.UpdateIssue(epic.UpdateJiraEpic(relatedJiraEpic.Fields.Labels, JiraSettings.GetPriorityIdFromMapping(jiraInstance.InstanceUrl, epic.Priority)), relatedJiraEpic.Key);
                    _log.DebugFormat("Updated Jira epic {0} with data from V1 epic {1}", relatedJiraEpic.Key, epic.Number);
                }

                processedEpics++;
            });

            _log.InfoFormat("Finished checking {0} V1 Epics", processedEpics);
            _log.Trace("Updating epics stopped");
        }

        public async Task CreateEpics(IJira jiraInstance)
        {
            _log.Trace("Creating epics started");
            var processedEpics = 0;
            var unassignedEpics = await _v1.GetEpicsWithoutReference(jiraInstance.V1Project, jiraInstance.EpicCategory);

            _log.DebugFormat("Found {0} epics to check for create", unassignedEpics.Count);

            unassignedEpics.ForEach(epic =>
            {
                _log.TraceFormat("Attempting to create Jira epic from {0}", epic.Number);
                var jiraData = jiraInstance.CreateEpic(epic, jiraInstance.JiraProject);

                _log.DebugFormat("Created Jira epic {0} from V1 epic {1}", jiraData.Key, epic.Number);

                if (jiraData.IsEmpty)
                {
                    _log.ErrorFormat(
                        "Saving epic failed. Possible reasons : Jira project ({0}) doesn't have epic type or expected custom field",
                        jiraInstance.JiraProject);
                }
                else
                {
                    jiraInstance.AddComment(jiraData.Key, string.Format(CreatedFromV1Comment, epic.Number, epic.ScopeName));
                    _log.TraceFormat("Added comment to Jira epic {0}", jiraData.Key);
                    jiraInstance.AddWebLink(jiraData.Key,
                        string.Format(V1AssetDetailWebLinkUrl, _v1.InstanceUrl, epic.Number),
                        string.Format(V1AssetDetailWebLinkTitle, epic.Number));
                    _log.TraceFormat("Added web link to Jira epic {0}", jiraData.Key);
                    epic.Reference = jiraData.Key;
                    _v1.UpdateEpicReference(epic);
                    _log.TraceFormat("Added reference in V1 epic {0}", epic.Number);
                    var link = jiraInstance.InstanceUrl + "/browse/" + jiraData.Key;
                    _v1.CreateLink(epic, string.Format("Jira {0}", jiraData.Key), link);
                    _log.TraceFormat("Added link in V1 epic {0}", epic.Number);
                    processedEpics++;
                }
            });

            _log.InfoFormat("Created {0} Jira epics", processedEpics);
            _log.Trace("Create epics stopped");
        }
    }
}
