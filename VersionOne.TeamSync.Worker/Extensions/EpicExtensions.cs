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
            return string.Equals(epic.Name, other.Fields.Summary) &&
                string.Equals(epic.Description, other.Fields.Description.ToEmptyIfNull()) &&
                string.Equals(epic.Reference, other.Key);
        }

        public static dynamic CreateJiraEpic(this Epic epic, string projectKey, string jiraEpicNameId)
        {
            dynamic expando = new ExpandoObject(); //not sure if this is entirely necessary ... ?

            expando.fields = new Dictionary<string, object>
            {
                { "description", epic.Description ?? "-"},
                { "summary", epic.Name},
                { "issuetype", new {name = "Epic"} },
                { "project", new {Key = projectKey}},
                { jiraEpicNameId,   epic.Name},
                {"labels", new List<string>() {epic.Number}}
            };

            return expando;
        }

        public static Issue UpdateJiraEpic(this Epic epic, List<string> labels)
        {
            return new Issue()
            {
                Fields = new Fields()
                {
                    Description = epic.Description ?? "-",
                    Summary =  epic.Name,
                    Labels = labels
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
