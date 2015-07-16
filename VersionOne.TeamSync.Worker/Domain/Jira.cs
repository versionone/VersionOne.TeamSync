using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using log4net;
using Newtonsoft.Json.Linq;
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
        bool ValidateConnection();
        bool ValidateProjectExists();

        void AddCreatedByV1Comment(string issueKey, string v1Number, string v1ProjectName, string v1Instance);
        void AddLinkToV1InComments(string issueKey, string v1Number, string v1ProjectName, string v1Instance);
        void AddCreatedAsVersionOneActualComment(string issueKey, string v1ActualOid, string v1WorkitemOid);

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

        IEnumerable<Worklog> GetIssueWorkLogs(string issueKey);
        void CleanUpAfterRun(ILog log);
    }

    public class Jira : IJira
    {
        private const string CreatedFromV1Comment = "Created from VersionOne Portfolio Item {0} in Project {1}\r\nURL:  {2}assetdetail.v1?Number={0}";
        private const string TrackedInV1 = "Tracking Issue {0} in Project {1}\r\nURL:  {2}assetdetail.v1?Number={0}";
        private const string CreatedAsVersionOneActualComment = "Created as VersionOne Actual \"{0}\" in Workitem \"{1}\"";
        private const int ConnectionAttempts = 3;

        private ILog _log;
        private ILog Log
        {
            get
            {
                return _log ?? LogManager.GetLogger(typeof(Jira));
            }
        }
        private readonly IJiraConnector _connector;
        private readonly string _jiraProject;
        private MetaProject _projectMeta;

        public string InstanceUrl { get; private set; }

        private MetaProject ProjectMeta
        {
            get
            {
                if (_projectMeta != null) return _projectMeta;
                var createMeta = _connector.GetCreateMetaInfoForProjects(new List<string> { _jiraProject });
                _projectMeta = createMeta.Projects.Single(p => p.Key == _jiraProject);

                return _projectMeta;
            }
        }

        public void CleanUpAfterRun(ILog log)
        {
            log.Info("Running cleanup...");
            _projectMeta = null;
        }

        public Jira(IJiraConnector connector, string jiraProject)
        {
            _connector = connector;
            _jiraProject = jiraProject;
            InstanceUrl = _connector.BaseUrl;
        }

        public Jira(IJiraConnector connector, MetaProject project, ILog log)
        {
            _connector = connector;
            _projectMeta = project;
            InstanceUrl = _connector.BaseUrl;
            _log = log;
        }

        public bool ValidateConnection()
        {
            for (var i = 0; i < ConnectionAttempts; i++)
            {
                Log.DebugFormat("Connection attempt {0}.", i + 1);

                if (_connector.IsConnectionValid())
                    return true;

                System.Threading.Thread.Sleep(5000);
            }
            return false;
        }

        public bool ValidateProjectExists()
        {
            return _connector.ProjectExists(_jiraProject);
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

        public void AddCreatedAsVersionOneActualComment(string issueKey, string v1ActualOid, string v1WorkitemOid)
        {
            var body = string.Format(CreatedAsVersionOneActualComment, v1ActualOid, v1WorkitemOid);
            _connector.Put(JiraResource.Issue.Value + "/" + issueKey, AddComment(body), HttpStatusCode.NoContent);
        }

        public void UpdateIssue(Issue issue, string issueKey)
        {
            _connector.Put("issue/" + issueKey, issue, HttpStatusCode.NoContent);
        }

        public void SetIssueToToDo(string issueKey)
        {
            Log.Info("Attempting to transition " + issueKey);

            _connector.Post("issue/{issueIdOrKey}/transitions", new
            {
                update = new
                {
                    comment = new[]
                    {
                        new
                        {
                            add = new
                            {
                                body = "This epic status is managed from VersionOne.  Done can only be set by a project admin"
                            }
                        }
                    }
                },
                transition = new { id = "11" }
            }, HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", issueKey));

            Log.Info(string.Format("Attempting to set status on {0}", issueKey));
        }

        public void SetIssueToResolved(string issueKey)
        {
            Log.Info("Attempting to transition " + issueKey);

            _connector.Post("issue/{issueIdOrKey}/transitions", new
            {
                update = new
                {
                    comment = new[]
                    {
                        new
                        {
                            add = new
                            {
                                body = "Closed from VersionOne"
                            }
                        }
                    }
                },
                transition = new { id = "31" }
            }, HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", issueKey));

            Log.Info(string.Format("Attempting to set status on {0}", issueKey));
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

        public SearchResult GetEpicsInProject(string projectKey)
        {
            return _connector.GetSearchResults(new List<JqOperator>
            {
                JqOperator.Equals("project", projectKey.QuoteReservedWord()),
                JqOperator.Equals("issuetype", "Epic")
            },
                new[] { "issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self" }
            );
        }

        public SearchResult GetEpicsInProjects(IEnumerable<string> projectKeys)
        {
            return _connector.GetSearchResults(new Dictionary<string, IEnumerable<string>>
            {
                {"project", projectKeys},
                {"issuetype", new[] {"Epic"}}
            },
                new[] { "issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self" }
            );
        }

        public ItemBase CreateEpic(Epic epic, string projectKey) // TODO: async
        {
            return _connector.Post<ItemBase>(JiraResource.Issue.Value, epic.CreateJiraEpic(projectKey, ProjectMeta.EpicName.Key), HttpStatusCode.Created);
        }

        public void DeleteEpicIfExists(string issueKey) // TODO: async
        {
            var existing = GetEpicByKey(issueKey);
            if (existing.HasErrors)
            {
                Log.Error(string.Format("Error attempting to remove jira issue {0}", issueKey));
                Log.Error(string.Format("  message(s) returned : {0}", string.Join(" ||| ", existing.ErrorMessages)));
                return;
            }

            _connector.Delete("issue/" + issueKey, HttpStatusCode.NoContent);
        }

        public SearchResult GetStoriesInProject(string jiraProject)
        {
            return _connector.GetSearchResults(new List<JqOperator>()
            {
               JqOperator.Equals("project", jiraProject.QuoteReservedWord()),
               JqOperator.Equals("issuetype", "Story")
            },
            new[] { "issuetype", "summary", "description", "priority", "status", "key", "self", "labels", "timetracking", ProjectMeta.StoryPoints.Key, ProjectMeta.EpicLink.Key },
            (issueKey, fields, properties) =>
            {
                properties.EvalLateBinding(issueKey, ProjectMeta.StoryPoints, value => fields.StoryPoints = value, Log);
                properties.EvalLateBinding(issueKey, ProjectMeta.EpicLink, value => fields.EpicLink = value, Log);
            });
        }

        public SearchResult GetStoriesWithNoEpicInProject(string projectKey)
        {
            return _connector.GetSearchResults(new List<JqOperator>()
            {
               JqOperator.Equals("project", projectKey.QuoteReservedWord()),
               JqOperator.Equals("issuetype", "Story"),
               JqOperator.Equals(ProjectMeta.EpicLink.Property.InQuotes(), JiraAdvancedSearch.Empty)
            },
            new[] { "issuetype", "summary", "description", "priority", "status", "key", "self", "labels", "timetracking", ProjectMeta.StoryPoints.Key },
            (issueKey, fields, properties) =>
            {
                properties.EvalLateBinding(issueKey, ProjectMeta.StoryPoints, value => fields.StoryPoints = value, Log);
            });
        }

        public SearchResult GetDefectsInProject(string jiraProject)
        {
            return _connector.GetSearchResults(new List<JqOperator>()
            {
               JqOperator.Equals("project", jiraProject.QuoteReservedWord()),
               JqOperator.Equals("issuetype", "Bug")
            },
            new[] { "issuetype", "summary", "description", "priority", "status", "key", "self", "labels", "timetracking", ProjectMeta.StoryPoints.Key, ProjectMeta.EpicLink.Key },
            (issueKey, fields, properties) =>
            {
                properties.EvalLateBinding(issueKey, ProjectMeta.StoryPoints, value => fields.StoryPoints = value, Log);
                properties.EvalLateBinding(issueKey, ProjectMeta.EpicLink, value => fields.EpicLink = value, Log);
            });
        }

        public IEnumerable<Worklog> GetIssueWorkLogs(string issueKey)
        {
            var content = _connector.Get("issue/{issueIdOrKey}/worklog", new KeyValuePair<string, string>("issueIdOrKey", issueKey));
            dynamic data = JObject.Parse(content);
            return ((JArray)data.worklogs).Select<dynamic, Worklog>(i => new Worklog
            {
                self = i.self,
                author = new Author
                {
                    self = i.author.self,
                    name = i.author.name,
                    key = i.author.key,
                    emailAddress = i.author.emailAddress,
                    displayName = i.author.displayName,
                    active = i.author.active,
                    timeZone = i.author.timeZone
                },
                updateAuthor = new Author
                {
                    self = i.updateAuthor.self,
                    name = i.updateAuthor.name,
                    key = i.updateAuthor.key,
                    emailAddress = i.updateAuthor.emailAddress,
                    displayName = i.updateAuthor.displayName,
                    active = i.updateAuthor.active,
                    timeZone = i.updateAuthor.timeZone
                },
                comment = i.comment,
                created = i.created,
                updated = i.updated,
                started = i.started,
                timeSpent = i.timeSpent,
                timeSpentSeconds = i.timeSpentSeconds,
                id = i.id
            });
        }

        private object AddComment(string body)
        {
            return new
            {
                update = new
                {
                    comment = new[]
                    {
                        new
                        {
                            add = new { body }
                        }
                    }
                }
            };
        }
    }
}