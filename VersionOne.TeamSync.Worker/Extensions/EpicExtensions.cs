using System.Collections.Generic;
using System.Dynamic;
using VersionOne.TeamSync.Core.Extensions;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.VersionOneWorker.Domain;

namespace VersionOne.TeamSync.JiraWorker.Extensions
{
    public static class EpicExtensions
    {
        public static bool ItMatches(this Epic epic, Issue other)
        {
            return string.Equals(epic.Name, other.Fields.Summary) &&
                string.Equals(epic.Description, other.Fields.Description.ToEmptyIfNull()) &&
                string.Equals(epic.Reference, other.Key);
        }

        public static object CreateJiraEpic(this Epic epic, string projectKey, string jiraEpicNameId, string priorityId)
        {
            dynamic data = new ExpandoObject();

            data.fields = new Dictionary<string, object>
            {
                { "description", epic.Description ?? "-"},
                { "summary", epic.Name},
                { "issuetype", new { name = "Epic" }},
                { "project", new { Key = projectKey }},
                { jiraEpicNameId, epic.Name},
                { "labels", new List<string> { epic.Number }}
            };
            if (!string.IsNullOrEmpty(priorityId))
                ((Dictionary<string, object>)data.fields).Add("priority", new { id = priorityId });

            return data;
        }

        public static object UpdateJiraEpic(this Epic epic, List<string> labels, string priorityId)
        {
            return new
            {
                fields = new
                {
                    description = epic.Description ?? "-",
                    summary = epic.Name,
                    priority = new { id = priorityId },
                    labels
                }
            };
        }

        public static void ReOpen(this Issue issue)
        {
            if (issue.Fields.Status == null)
                issue.Fields.Status = new Status { Name = "ToDo" };
            else
                issue.Fields.Status.Name = "ToDo";
        }
    }
}
