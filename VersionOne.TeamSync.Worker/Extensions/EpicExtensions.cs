using System.Collections.Generic;
using System.Dynamic;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker.Extensions
{
    public static class EpicExtensions
    {
        public static bool ItMatches(this Epic epic, Issue other)
        {
            string dataValue = epic.Number + " " + epic.Name;

            return string.Equals(dataValue, other.Fields.Summary) &&
                string.Equals(epic.Description, other.Fields.Description.ToEmptyIfNull()) &&
                string.Equals(epic.Reference, other.Key);
        }

        public static dynamic CreateJiraEpic(this Epic epic, string projectKey, string jiraEpicNameId)
        {
            dynamic expando = new ExpandoObject(); //not sure if this is entirely necessary ... ?

            string dataValue = epic.Number + " " + epic.Name;

            expando.fields = new Dictionary<string, object>
            {
                { "description", epic.Description ?? "-"},
                { "summary", dataValue},
                { "issuetype", new {name = "Epic"} },
                { "project", new {Key = projectKey}},
                { jiraEpicNameId,   dataValue}
            };

            return expando;
        }

        public static Issue UpdateJiraEpic(this Epic epic)
        {
            return new Issue()
            {
                Fields = new Fields()
                {
                    Description = epic.Description ?? "-",
                    Summary = epic.Name,
                }
            };
        }

        public static void ReOpen(this Issue issue)
        {
            if (issue.Fields.Status == null)
                issue.Fields.Status = new Status() { Name = "ToDo" };
            else
                issue.Fields.Status.Name = "ToDo";
        }
    }
}
