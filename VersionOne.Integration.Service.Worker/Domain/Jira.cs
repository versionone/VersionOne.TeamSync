﻿using System;
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
    public interface IJira
    {
        ItemBase CreateEpic(Epic epic, string projectKey);
        void AddCreatedByV1Comment(string issueKey, Epic epic, string v1Instance);
        void UpdateEpic(Epic epic, string issueKey);
        void DeleteEpicIfExists(string issueKey);
        SearchResult GetEpicsInProject(string projectKey);
        SearchResult GetEpicByKey(string reference);
        void SetIssueToResolved(string issueKey);
        string InstanceUrl { get; }
    }

    public class Jira : IJira
    {
        private readonly IJiraConnector _connector;

        public Jira(JiraConnector connector)
        {
            _connector = connector;
            InstanceUrl = _connector.BaseUrl;
        }

        public Jira(IJiraConnector connector)
        {
            _connector = connector;
            InstanceUrl = _connector.BaseUrl;
        }

        public ItemBase CreateEpic(Epic epic, string projectKey) // TODO: async
        {
            var baseItem = _connector.Post(JiraResource.Issue.Value, epic.CreateJiraEpic(projectKey), HttpStatusCode.Created);
            return baseItem;
        }

        public void AddCreatedByV1Comment(string issueKey, Epic epic, string v1Instance)
        {
            _connector.Put(JiraResource.Issue.Value + "/" + issueKey, CreatedByV1Comment(epic, v1Instance), HttpStatusCode.NoContent);
        }

        private object CreatedByV1Comment(Epic epic, string v1Instance)
        {
            return new
            {
                update = new {
                    comment = new[]
                    {
                        new { add = new { body = string.Format("Created from VersionOne Portfolio Item {0} in Project {1}\r\nURL:  {2}assetdetail.v1?Number={0}", epic.Number, epic.ProjectName, v1Instance)} }
                    }
                }
            };
        }

        public void UpdateEpic(Epic epic, string issueKey) // TODO: async
        {
            _connector.Put("issue/" + issueKey, epic.UpdateJiraEpic(), HttpStatusCode.NoContent);
        }

        public void DeleteEpicIfExists(string issueKey) // TODO: async
        {
            var existing = GetEpicByKey(issueKey);
            if (existing.HasErrors)
            {
                SimpleLogger.WriteLogMessage("Error attempting to remove jira issue " + issueKey);
                SimpleLogger.WriteLogMessage("  message(s) returned : " + string.Join(" ||| ", existing.ErrorMessages));
                return;
            }

            _connector.Delete("issue/" + issueKey, HttpStatusCode.NoContent);
        }

        public SearchResult GetEpicsInProject(string projectKey)
        {
            return _connector.GetSearchResults(new Dictionary<string, string>
                {
                    {"project", projectKey},
                    {"issuetype", "Epic"}
                },
                    new[] {"issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self"}
                );
        }

        public SearchResult GetEpicByKey(string reference)
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

        public string InstanceUrl { get; private set; }
    }
}