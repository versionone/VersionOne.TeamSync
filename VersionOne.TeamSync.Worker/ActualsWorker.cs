using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker
{
    public class ActualsWorker : IAsyncWorker
    {
        private const string PluralAsset = "actuals";
        private const string CreatedAsVersionOneActualComment = "Created as VersionOne {0} in Workitem {1}";

        private bool _isActualWorkEnabled;
        private readonly IV1 _v1;
        private readonly ILog _log;

        public ActualsWorker(IV1 v1, ILog log)
        {
            _v1 = v1;
            _log = log;
        }

        public async Task DoWork(V1JiraInfo jiraInfo)
        {
            // TODO: check if we can do this validation only on startup
            ValidateRequiredV1Fields();

            if (!_isActualWorkEnabled)
                return;

            var allJiraDefects = jiraInfo.JiraInstance.GetDefectsInProject(jiraInfo.JiraKey).issues;
            var allV1Defects = await _v1.GetDefectsWithJiraReference(jiraInfo.V1ProjectId);
            DoActualWork(jiraInfo, allJiraDefects, allV1Defects);

            var allJiraStories = jiraInfo.JiraInstance.GetStoriesInProject(jiraInfo.JiraKey).issues;
            var allV1Stories = await _v1.GetStoriesWithJiraReference(jiraInfo.V1ProjectId);
            DoActualWork(jiraInfo, allJiraStories, allV1Stories);

        }

        public void DoActualWork<T>(V1JiraInfo jiraInfo, IEnumerable<Issue> allJiraIssues, List<T> allV1WorkItems) where T : IPrimaryWorkItem
        {
            foreach (var issueKey in allJiraIssues.Select(issue => issue.Key))
            {
                var workItem = allV1WorkItems.FirstOrDefault(s => s.Reference.Equals(issueKey));
                if (workItem == null || workItem.Equals(default(T))) continue;

                _log.TraceFormat("Getting Jira Worklogs for Issue key: {0}", issueKey);
                var worklogs = jiraInfo.JiraInstance.GetIssueWorkLogs(issueKey).ToList();

                _log.TraceFormat("Getting V1 actuals for Workitem Oid: {0}", workItem.Oid());
                var actuals = _v1.GetWorkItemActuals(jiraInfo.V1ProjectId, workItem.Oid()).Result.ToList();

                _log.Trace("Creating actuals started");
                var newWorklogs = worklogs.Where(w => !actuals.Any(a => a.Reference.Equals(w.id.ToString()))).ToList();
                if (newWorklogs.Any())
                    CreateActualsFromWorklogs(jiraInfo, newWorklogs, workItem.Oid(), workItem.Number, issueKey);
                _log.TraceCreateFinished(PluralAsset);

                _log.Trace("Updating actual started");
                var updateWorklogs = worklogs.Where(w => actuals.Any(a => a.Reference.Equals(w.id.ToString()) &&
                    // Have started date changed?
                    (!w.started.ToString(CultureInfo.InvariantCulture).Equals(a.Date.ToString(CultureInfo.InvariantCulture)) ||
                    // Have worked hours changed?
                    Convert.ToInt32(double.Parse(a.Value) * 3600) != w.timeSpentSeconds ||
                    // Have updated author changed?
                    !ActualMemberMatchesWorklogUpdateAuthor(w, a)
                    )
                    )).ToList();
                if (updateWorklogs.Any())
                    UpdateActualsFromWorklogs(jiraInfo, updateWorklogs, workItem.Oid(), actuals);
                _log.TraceUpdateFinished(PluralAsset);

                _log.Trace("Deleting actuals started");
                var actualsToDelete = actuals.Where(a => !worklogs.Any(w => w.id.ToString().Equals(a.Reference)) &&
                                                         !a.Value.Equals("0")).ToList();
                if (actualsToDelete.Any())
                    DeleteActualsFromWorklogs(actualsToDelete);
                _log.TraceDeleteFinished(PluralAsset);
            }
        }

        public void CreateActualsFromWorklogs(V1JiraInfo jiraInfo, List<Worklog> newWorklogs, string workItemId, string v1Number, string issueKey)
        {
            _log.DebugFormat("Found {0} worklogs to check for create", newWorklogs.Count());
            var processedActuals = 0;
            foreach (var worklog in newWorklogs)
            {
                _log.TraceFormat("Attempting to create actual from Jira worklog id {0}", worklog.id);
                var member = _v1.SyncMemberFromJiraUser(worklog.updateAuthor).Result;
                var actual = worklog.ToV1Actual(member.Oid(), jiraInfo.V1ProjectId, workItemId);
                var newActual = _v1.CreateActual(actual).Result;
                _log.DebugFormat("Created V1 actual id {0} from Jira worklog id {1}", newActual.ID, worklog.id);

                jiraInfo.JiraInstance.AddComment(issueKey, string.Format(CreatedAsVersionOneActualComment, newActual.Oid(), v1Number));
                _log.TraceFormat("Added comment on Jira worklog id {0} with new V1 actual id {1}", worklog.id, newActual.ID);

                processedActuals++;
            }
            if (processedActuals > 0) _log.InfoCreated(processedActuals, PluralAsset);
        }

        public void UpdateActualsFromWorklogs(V1JiraInfo jiraInfo, List<Worklog> updateWorklogs, string workItemId, List<Actual> actuals)
        {
            _log.DebugFormat("Found {0} worklogs to check for update", updateWorklogs.Count());
            var processedActuals = 0;
            foreach (var worklog in updateWorklogs)
            {
                var member = _v1.SyncMemberFromJiraUser(worklog.updateAuthor).Result;
                var actual = worklog.ToV1Actual(member.Oid(), jiraInfo.V1ProjectId, workItemId);
                actual.ID = actuals.Single(a => a.Reference.Equals(worklog.id.ToString())).ID;

                _log.TraceFormat("Attempting to update actual id {0} from Jira worklog id {1}", actual.ID, worklog.id);
                _v1.UpdateAsset(actual, actual.CreatePayload());
                _log.DebugFormat("Updated V1 actual id {0}", actual.ID);

                processedActuals++;
            }
            if (processedActuals > 0) _log.InfoUpdated(processedActuals, PluralAsset);
        }

        public void DeleteActualsFromWorklogs(List<Actual> actualsToDelete)
        {
            _log.DebugFormat("Found {0} actuals to check for delete", actualsToDelete.Count);
            var processedActuals = 0;
            foreach (var actual in actualsToDelete)
            {
                _log.TraceFormat("Attempting to update actual id {0} with value 0", actual.ID);
                actual.Value = "0";
                _v1.UpdateAsset(actual, actual.CreatePayload());
                _log.DebugFormat("Updated V1 actual id {0}", actual.ID);

                processedActuals++;
            }
           if (processedActuals > 0) _log.InfoDelete(processedActuals, PluralAsset);
        }

        private void ValidateRequiredV1Fields()
        {
            _log.Info("Verifying VersionOne required fields...");
            if (!(_isActualWorkEnabled = _v1.ValidateActualReferenceFieldExists()))
            {
                _log.Warn("Actual.Reference field is missing in VersionOne instance. JIRA worklogs will not be synced.");
            }
        }

        private bool ActualMemberMatchesWorklogUpdateAuthor(Worklog worklog, Actual actual)
        {
            var member = _v1.GetMember(worklog.updateAuthor.name).Result;
            return member != null && member.Oid().Equals(actual.MemberId) && worklog.updateAuthor.ItMatchesMember(member);
        }
    }
}
