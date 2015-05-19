using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VersionOne.Integration.Service.Core;
using VersionOne.Integration.Service.Worker.Extensions;
using VersionOne.SDK.Jira.Connector;
using VersionOne.SDK.Jira.Entities;

namespace VersionOne.Integration.Service.Worker.Domain
{
    public class Jira
    {
        private readonly JiraConnector _connector;

        public Jira(JiraConnector connector)
        {
            _connector = connector;
        }

        internal ItemBase CreateEpic(Epic epic, string projectKey) // TODO: async
        {
            return _connector.Post(JiraResource.Issue.Value, epic.CreateJiraEpic(projectKey), HttpStatusCode.Created);
        }

        internal void UpdateEpic(Epic epic, string issueKey) // TODO: async
        {
            _connector.Put("issue/" + issueKey, epic.UpdateJiraEpic(), HttpStatusCode.NoContent);
        }

        internal async void ResolveEpic(Epic epic) // TODO: async
        {
        }

        internal async void DeleteEpic(string issueKey) // TODO: async
        {
            _connector.Delete("issue/" + issueKey, HttpStatusCode.NoContent);
        }

        internal SearchResult GetEpicsInProject(string projectKey)
        {
            return _connector.GetSearchResults(new Dictionary<string, string>
                {
                    {"project", projectKey},
                    {"issuetype", "Epic"}
                },
                    new[] {"issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self"}
                );
        }

        internal SearchResult GetEpicByKey(string reference)
        {
            return _connector.GetSearchResults(new Dictionary<string, string>
            {
                {"key", reference},
                {"issuetype", "Epic"}
            },
                new[] {"issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self"}
                );
        }

        public void SetIssueToResolved(string issueKey)
        {
            SimpleLogger.WriteLogMessage("Attempting to transition " + issueKey);

            _connector.Post("issue/" + issueKey + "/transitions", new
            {
                update = new
                {
                    comment = new[]
                    {
                        new {add = new {body = "Closed from VersionOne"}}
                    }
                },
                transition = new {id = "31"}
            }, HttpStatusCode.NoContent);

            SimpleLogger.WriteLogMessage("Attempting to set status on " + issueKey);

            _connector.Put("issue/" + issueKey, new
            {
                update = new
                {
                    customfield_10007 = new[]
                    {
                        new {set = new {value = "Done"}}
                    }
                }
            }, HttpStatusCode.NoContent);
        }
    }
}
