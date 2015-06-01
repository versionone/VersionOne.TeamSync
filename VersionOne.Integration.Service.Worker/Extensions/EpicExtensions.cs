using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.JiraConnector.Entities;

namespace VersionOne.Integration.Service.Worker.Extensions
{
    public static class EpicExtensions
    {
        public static Issue CreateJiraEpic(this Epic epic, string projectKey)
        {
            return new Issue()
            {
                Fields = new EpicFields()
                {
                    Description = epic.Description ?? "-",
                    Summary = epic.Name,
                    Name = epic.Number,
                    IssueType = new IssueType() {Name = "Epic"},
                    Project = new Project() {Key = projectKey}
                }
            };
        }

        public static Issue UpdateJiraEpic(this Epic epic)
        {
            return new Issue()
            {
                Fields = new EpicFields()
                {
                    Description = epic.Description ?? "-",
                    Summary = epic.Name,
                    Name = epic.Number,
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
