using System.Collections.Generic;
using System.Dynamic;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker.Extensions
{
    public static class EpicExtensions
    {
        public static dynamic CreateJiraEpic(this Epic epic, string projectKey, string jiraEpicNameId)
        {
            dynamic expando = new ExpandoObject(); //not sure if this is entirely necessary ... ?

            expando.fields = new Dictionary<string, object>
            {
                { "description", epic.Description ?? "-"},
                { "summary", epic.Name},
                { "issuetype", new {name = "Epic"} },
                { "project", new {Key = projectKey}},
                { jiraEpicNameId, epic.Number}
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
                issue.Fields.Status = new Status() {Name = "ToDo"};
            else
                issue.Fields.Status.Name = "ToDo";
        }
    }
}
