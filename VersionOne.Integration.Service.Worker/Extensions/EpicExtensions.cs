using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.SDK.Jira.Entities;

namespace VersionOne.Integration.Service.Worker.Extensions
{
    public static class EpicExtensions
    {
        public static Issue ToJiraEpic(this Epic epic, string projectKey)
        {
            return new Issue()
            {
                Fields = new EpicFields()
                {
                    Description = epic.Description,
                    Summary = epic.Name,
                    IssueType = new IssueType() {Name = "Epic"},
                    Project = new Project() {Key = projectKey}
                }
            };
        }
    }
}
