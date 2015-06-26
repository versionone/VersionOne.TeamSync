using System.Collections.Generic;
using System.Linq;
using System.Net;
using log4net;
using VersionOne.TeamSync.JiraConnector;
using VersionOne.TeamSync.JiraConnector.Connector;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraConnector.Interfaces;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Worker.Domain
{
    public interface IJira
    {
        string InstanceUrl { get; }
        void ValidateConnection();
        bool ValidateProjectExists();

        void AddCreatedByV1Comment(string issueKey, string v1Number, string v1ProjectName, string v1Instance);
        void AddLinkToV1InComments(string issueKey, string v1Number, string v1ProjectName, string v1Instance);

        void UpdateIssue(Issue issue, string issueKey);
        void SetIssueToToDo(string issueKey);
        void SetIssueToResolved(string issueKey);

        SearchResult GetEpicByKey(string reference);
        SearchResult GetEpicsInProject(string projectKey);
        SearchResult GetEpicsInProjects(IEnumerable<string> projectKeys);
        ItemBase CreateEpic(Epic epic, string projectKey);
        void DeleteEpicIfExists(string issueKey);

        SearchResult GetStoriesInProject(string jiraProject);
        SearchResult GetStoriesWithNoEpicInProject(string projectKey);

        SearchResult GetDefectsInProject(string jiraProject);
    }

    public class Jira : IJira
    {
        private const string CreatedFromV1Comment = "Created from VersionOne Portfolio Item {0} in Project {1}\r\nURL:  {2}assetdetail.v1?Number={0}";
        private const string TrackedInV1 = "Tracking Issue {0} in Project {1}\r\nURL:  {2}assetdetail.v1?Number={0}";
        private const int ConnectionAttempts = 3;

        private readonly IJiraConnector _connector;

        private static ILog _log = LogManager.GetLogger(typeof(Jira));
        private MetaProject _projectMeta;
        private string _jiraProject;

        public Jira(IJiraConnector connector, string jiraProject)
        {
            _connector = connector;
            _jiraProject = jiraProject;
            InstanceUrl = _connector.BaseUrl;
        }

        public Jira(IJiraConnector connector, MetaProject project)
        {
            _connector = connector;
            _projectMeta = project;
            InstanceUrl = _connector.BaseUrl;
        }

        private MetaProject ProjectMeta
        {
            get
            {
                if (_projectMeta == null)
                {
                    var createMeta = _connector.GetCreateMetaInfoForProjects(new List<string>() { _jiraProject });
                    _projectMeta = createMeta.Projects.Single(p => p.Key == _jiraProject);
                }

                return _projectMeta;
            }
        }

        private object AddComment(string body)
        {
            return new
            {
                update = new
                {
                    comment = new[]
                    {
                        new { add = new { body} }
                    }
                }
            };
        }

        public ItemBase CreateEpic(Epic epic, string projectKey) // TODO: async
        {
            var baseItem = _connector.Post(JiraResource.Issue.Value, epic.CreateJiraEpic(projectKey, ProjectMeta.EpicName.Key), HttpStatusCode.Created);
            return baseItem;
        }

        public void AddCreatedByV1Comment(string issueKey, string v1Number, string v1ProjectName, string v1Instance)
        {
            var body = string.Format(CreatedFromV1Comment, v1Number, v1ProjectName, v1Instance);
            _connector.Put(JiraResource.Issue.Value + "/" + issueKey, AddComment(body), HttpStatusCode.NoContent);
        }

        public void AddLinkToV1InComments(string issueKey, string v1Number, string v1ProjectName, string v1Instance)
        {
            var body = string.Format(TrackedInV1, v1Number, v1ProjectName, v1Instance);
            _connector.Put(JiraResource.Issue.Value + "/" + issueKey, AddComment(body), HttpStatusCode.NoContent);
        }

        public void UpdateIssue(Issue issue, string issueKey)
        {
            _connector.Put("issue/" + issueKey, issue, HttpStatusCode.NoContent);
        }

        public SearchResult GetStoriesInProject(string jiraProject)
        {
            return _connector.GetSearchResults(new List<JqOperator>()
            {
               JqOperator.Equals("project", jiraProject.QuoteReservedWord()),
               JqOperator.Equals("issuetype", "Story"),
            },
            new[] { "issuetype", "summary", "description", "priority", "status", "key", "self", "labels", "timetracking", ProjectMeta.StoryPoints.Key, ProjectMeta.EpicLink.Key },
            (fields, properties) =>
            {
                if (properties.ContainsKey(ProjectMeta.StoryPoints.Key) && properties[ProjectMeta.StoryPoints.Key] != null)
                    fields.StoryPoints = properties[ProjectMeta.StoryPoints.Key].ToString();
                if (properties.ContainsKey(ProjectMeta.EpicLink.Key) && properties[ProjectMeta.EpicLink.Key] != null)
                    fields.EpicLink = properties[ProjectMeta.EpicLink.Key].ToString();
            });
        }

        public SearchResult GetDefectsInProject(string jiraProject)
        {
            return _connector.GetSearchResults(new List<JqOperator>()
            {
               JqOperator.Equals("project", jiraProject.QuoteReservedWord()),
               JqOperator.Equals("issuetype", "Bug"),
            },
            new[] { "issuetype", "summary", "description", "priority", "status", "key", "self", "labels", "timetracking", ProjectMeta.StoryPoints.Key, ProjectMeta.EpicLink.Key },
            (fields, properties) =>
            {
                //exception!
                if (properties.ContainsKey(ProjectMeta.StoryPoints.Key) && properties[ProjectMeta.StoryPoints.Key] != null)
                    fields.StoryPoints = properties[ProjectMeta.StoryPoints.Key].ToString();
                if (properties.ContainsKey(ProjectMeta.EpicLink.Key) && properties[ProjectMeta.EpicLink.Key] != null)
                    fields.EpicLink = properties[ProjectMeta.EpicLink.Key].ToString();
            });
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
                    new[] { "issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self" }
                );
        }

        public SearchResult GetEpicsInProjects(IEnumerable<string> projectKeys)
        {
            return _connector.GetSearchResults(new Dictionary<string, IEnumerable<string>>
            {
                {"project", projectKeys},
                {"issuetype", new[] {"Epic"}},
            },
                new[] { "issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self" }
            );
        }

        public SearchResult GetStoriesWithNoEpicInProject(string projectKey)
        {
            return _connector.GetSearchResults(new List<JqOperator>()
            {
               JqOperator.Equals("project", projectKey.QuoteReservedWord()),
               JqOperator.Equals("issuetype", "Story"),
               JqOperator.Equals(ProjectMeta.EpicLink.Property.InQuotes(), JiraAdvancedSearch.Empty),
            },
            new[] { "issuetype", "summary", "description", "priority", "status", "key", "self", "labels", "timetracking", ProjectMeta.StoryPoints.Key },
            (fields, properties) =>
            {
                if (properties.ContainsKey(ProjectMeta.StoryPoints.Key))
                    fields.StoryPoints = properties[ProjectMeta.StoryPoints.Key].ToString();
            });
        }

        public SearchResult GetEpicByKey(string reference)
        {
            return _connector.GetSearchResults(new List<JqOperator>
            {
                JqOperator.Equals("key", reference),
                JqOperator.Equals("issuetype", "Epic")
            },
                new[] { "issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self" }
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
                transition = new { id = "31" }
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

        public void ValidateConnection()
        {
            for (var i = 0; i < ConnectionAttempts; i++)
            {
                _log.DebugFormat("Connection attempt {0}.", i + 1);

                if (!_connector.IsConnectionValid())
                {
                    System.Threading.Thread.Sleep(5000);
                }
                else
                {
                    _log.Info("Jira connection successful!");
                    return;
                }
            }
        }

        public bool ValidateProjectExists()
        {
            return _connector.ProjectExists(_jiraProject);
        }
    }
}
