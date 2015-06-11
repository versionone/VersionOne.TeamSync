using System.Collections.Generic;
using System.Net;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.JiraConnector;
using VersionOne.TeamSync.JiraConnector.Connector;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public interface IJira
    {
        ItemBase CreateEpic(Epic epic, string projectKey);
        void AddCreatedByV1Comment(string issueKey, Epic epic, string v1Instance);
        void UpdateEpic(Epic epic, string issueKey);
        void DeleteEpicIfExists(string issueKey);
        SearchResult GetEpicsInProject(string projectKey);
        SearchResult GetEpicsInProjects(IEnumerable<string> projectKeys);
        SearchResult GetEpicByKey(string reference);
        void SetIssueToResolved(string issueKey);
        void SetIssueToToDo(string issueKey);

        string InstanceUrl { get; }
        SearchResult GetStoriesWithNoEpicInProject(string projectKey);
    }

    public class Jira : IJira
    {
        private readonly IJiraConnector _connector;
       
        private static ILog _log = LogManager.GetLogger(typeof (Jira));
        private MetaProject _projectMeta;

        public Jira(JiraConnector.Connector.JiraConnector connector, MetaProject project)
        {
            _connector = connector;
            _projectMeta = project;
            InstanceUrl = _connector.BaseUrl;
        }

        public Jira(IJiraConnector connector, MetaProject project)
        {
            _connector = connector;
            _projectMeta = project;
            InstanceUrl = _connector.BaseUrl;
        }

        public ItemBase CreateEpic(Epic epic, string projectKey) // TODO: async
        {
            var baseItem = _connector.Post(JiraResource.Issue.Value, epic.CreateJiraEpic(projectKey, _projectMeta.EpicName.Key), HttpStatusCode.Created);
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
                _log.Error("Error attempting to remove jira issue " + issueKey);
                _log.Error("  message(s) returned : " + string.Join(" ||| ", existing.ErrorMessages));
                return;
            }

            _connector.Delete("issue/" + issueKey, HttpStatusCode.NoContent);
        }

        public SearchResult GetEpicsInProject(string projectKey)
        {
            return _connector.GetSearchResults(new List<JqOperator>
                {
                    JqOperator.Equals("project", projectKey.QuoteReservedWord()),
                    JqOperator.Equals("issuetype", "Epic"),
                },
                    new[] {"issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self"}
                );
        }

        public SearchResult GetEpicsInProjects(IEnumerable<string> projectKeys)
        {
            return _connector.GetSearchResults(new Dictionary<string, IEnumerable<string>>
            {
                {"project", projectKeys},
                {"issuetype", new[] {"Epic"}},
            },
                new[] {"issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self"}
            );
        }

        public SearchResult GetStoriesWithNoEpicInProject(string projectKey)
        {
            return _connector.GetSearchResults(new List<JqOperator>()
            {
               JqOperator.Equals("project", projectKey.QuoteReservedWord()),
               JqOperator.Equals("issuetype", "Story"),
               JqOperator.Equals(_projectMeta.EpicLink.Property.InQuotes(), JiraAdvancedSearch.Empty),
            },
            new[] { "issuetype", "summary", "description", "priority", "status", "key","self", _projectMeta.StoryPoints.Key },
                (fields, properties) =>
                {
                    if (properties.ContainsKey(_projectMeta.StoryPoints.Key))
                        fields.StoryPoints = properties[_projectMeta.StoryPoints.Key].ToString();
                });
        }

        public SearchResult GetEpicByKey(string reference)
        {
            return _connector.GetSearchResults(new List<JqOperator>
            {
                JqOperator.Equals("key", reference),
                JqOperator.Equals("issuetype", "Epic")
            },
                new[] {"issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self"}
                );
        }

        public void SetIssueToResolved(string issueKey)
        {
            _log.Info("Attempting to transition " + issueKey);

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

            _log.Info("Attempting to set status on " + issueKey);
        }

        public void SetIssueToToDo(string issueKey)
        {
            _log.Info("Attempting to transition " + issueKey);

            _connector.Post("issue/" + issueKey + "/transitions", new
            {
                update = new
                {
                    comment = new[]
                    {
                        new {add = new {body = "This epic status is managed from VersionOne.  Done can only be set by a project admin"}}
                    }
                },
                transition = new { id = "11" }
            }, HttpStatusCode.NoContent);

            _log.Info("Attempting to set status on " + issueKey);
        }

        public string InstanceUrl { get; private set; }
    }
}
