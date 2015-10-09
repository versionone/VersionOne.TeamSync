using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using log4net;
using Newtonsoft.Json.Linq;
using VersionOne.TeamSync.JiraConnector;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraConnector.Interfaces;
using VersionOne.TeamSync.Worker.Extensions;
using Connector = VersionOne.TeamSync.JiraConnector.Connector;

namespace VersionOne.TeamSync.Worker.Domain
{
    public interface IJira
    {
        string InstanceUrl { get; }
        string Username { get; }

        JiraVersionInfo VersionInfo { get; }
        string JiraProject { get; }
        string V1Project { get; }
        string EpicCategory { get; }
        string[] DoneWords { get; }

        bool ValidateConnection();
        bool ValidateProjectExists();
        bool ValidateMemberPermissions();

        string GetPriorityId(string name);

        void AddComment(string issueKey, string comment);
        void AddWebLink(string issueKey, string webLinkUrl, string webLinkTitle);

        void UpdateIssue(object issue, string issueKey);
        void SetIssueToToDo(string issueKey, string[] doneWords);
        void SetIssueToResolved(string issueKey, string[] doneWords);

        SearchResult GetEpicByKey(string reference);
        SearchResult GetEpicsInProject(string projectKey);
        SearchResult GetEpicsInProjects(IEnumerable<string> projectKeys);
        ItemBase CreateEpic(Epic epic, string projectKey);
        void DeleteEpicIfExists(string issueKey);

        SearchResult GetStoriesInProject(string jiraProject);
        SearchResult GetStoriesWithNoEpicInProject(string projectKey);

        SearchResult GetBugsInProject(string jiraProject);

        IEnumerable<Worklog> GetIssueWorkLogs(string issueKey);
        string GetIssueTransitionId(string issueKey, string toState);
        void RunTransitionOnIssue(string transitionId, string issueKey);

        void CleanUpAfterRun(ILog log);
        IJiraSettings JiraSettings { get; }
        SearchResult GetAllStoriesInProjectSince(string jiraProject, string date);

        string RunFromThisDateOn { get; }
        SearchResult GetAllBugsInProjectSince(string jiraProject, string createdDate);
    }

    public class Jira : IJira
    {
        private const int ConnectionAttempts = 3;

        private readonly ILog _log;
        private readonly IJiraConnector _connector;
        private MetaProject _projectMeta;
        private JiraVersionInfo _jiraVersionInfo;
        private string[] _doneWords;
        private IJiraSettings _jiraSettings;

        public string InstanceUrl { get; private set; }

        public string Username
        {
            get { return _connector.Username; }
        }

        public string JiraProject { get; private set; }

        public string V1Project { get; private set; }

        public string EpicCategory { get; private set; }

        public string RunFromThisDateOn { get; private set; }

        public string[] DoneWords
        {
            get { return _doneWords ?? (_doneWords = JiraVersionItems.VersionDoneWords[VersionInfo.VersionNumbers[0]]); }
        }

        public IJiraSettings JiraSettings
        {
            get { return _jiraSettings ?? JiraConnector.Config.JiraSettings.GetInstance(); }
            private set { _jiraSettings = value; }
        }

        public Jira(IJiraSettings jiraSettings)
        {
            _jiraSettings = jiraSettings;
        }

        public Jira(IJiraConnector connector)
        {
            _connector = connector;
            InstanceUrl = _connector.BaseUrl;
        }

        public Jira(IJiraConnector connector, ProjectMapping projectMapping, string runFromThisDateOn) : this(connector)
        {
            JiraProject = projectMapping.JiraProject;
            V1Project = projectMapping.V1Project;
            EpicCategory = projectMapping.EpicSyncType;
            RunFromThisDateOn = runFromThisDateOn;
            _log = LogManager.GetLogger(typeof(Jira));
        }

        public Jira(IJiraConnector connector, MetaProject project, ILog log)
            : this(connector)
        {
            _projectMeta = project;
            _log = log;
        }

        public bool ValidateConnection()
        {
            for (var i = 0; i < ConnectionAttempts; i++)
            {
                _log.DebugFormat("Connection attempt {0}.", i + 1);

                if (_connector.IsConnectionValid())
                    return true;

                System.Threading.Thread.Sleep(5000);
            }
            return false;
        }

        public bool ValidateProjectExists()
        {
            return _connector.ProjectExists(JiraProject);
        }

        public bool ValidateMemberPermissions()
        {
            var userInfo = _connector.GetUserInfo();
            if (userInfo == null)
                return false;

            return userInfo.Groups.Items.Any(
                item => item.Name.Equals("jira-administrators") || item.Name.Equals("jira-developers"));
        }

        public string GetPriorityId(string name)
        {
            var path = string.Format("{0}/priority", Connector.JiraConnector.JiraRestApiUrl);
            var content = _connector.Get(path);
            var data = JArray.Parse(content);
            return data.Where<dynamic>(i => i.name == name).Select(i => i.id).FirstOrDefault();
        }

        public JiraVersionInfo VersionInfo
        {
            get { return _jiraVersionInfo ?? (_jiraVersionInfo = _connector.GetVersionInfo()); }
        }

        public void AddComment(string issueKey, string comment)
        {
            var path = string.Format("{0}/issue/{{issueIdOrKey}}", Connector.JiraConnector.JiraRestApiUrl);
            _connector.Put(path, AddComment(comment), HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", issueKey));
        }

        public void AddWebLink(string issueKey, string webLinkUrl, string webLinkTitle)
        {
            var path = string.Format("{0}/issue/{{issueIdOrKey}}/remotelink", Connector.JiraConnector.JiraRestApiUrl);
            var body = new { @object = new { url = webLinkUrl, title = webLinkTitle } };
            _connector.Post(path, body, HttpStatusCode.Created, new KeyValuePair<string, string>("issueIdOrKey", issueKey));
        }

        public void UpdateIssue(object issue, string issueKey)
        {
            var path = string.Format("{0}/issue/{{issueIdOrKey}}", Connector.JiraConnector.JiraRestApiUrl);
            _connector.Put(path, issue, HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", issueKey));
        }

        public void SetIssueToToDo(string issueKey, string[] doneWords)
        {
            _log.Info("Attempting to transition " + issueKey);

            var path = string.Format("{0}/issue/{{issueIdOrKey}}/transitions", Connector.JiraConnector.JiraRestApiUrl);
            var content = _connector.Get<TransitionResponse>(path, new KeyValuePair<string, string>("issueIdOrKey", issueKey));
            var transition = content.Transitions.Where(t => !doneWords.Contains(t.Name)).ToList();
            if (transition.Count == 0)
            {
                _log.Error("No transitions found.  This jira epic will not be updated");
                return;
            }
            _log.Info("Attempting to transition " + issueKey);

            _connector.Post(path, new
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
                transition = new { id = transition.First().Id }
            }, HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", issueKey));

            _log.Info(string.Format("Attempting to set status on {0}", issueKey));
        }

        public void SetIssueToResolved(string issueKey, string[] doneWords)
        {
            _log.Info("Attempting to transition " + issueKey);

            var path = string.Format("{0}/issue/{{issueIdOrKey}}/transitions", Connector.JiraConnector.JiraRestApiUrl);
            var content = _connector.Get<TransitionResponse>(path, new KeyValuePair<string, string>("issueIdOrKey", issueKey));
            var transition = content.Transitions.Where(t => doneWords.Contains(t.Name)).ToList();
            if (transition.Count != 1)
            {
                _log.Error("None or multiple transistions exists for " + issueKey + " with the status of " + string.Join(" or ", doneWords) + ".  This epic will not be updated");
                return;
            }

            _connector.Post(path, new
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
                transition = new { id = transition.Single().Id }
            }, HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", issueKey));

            _log.Info(string.Format("Set status on {0} to {1}", issueKey, transition.Single().Name));
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
                new[] { "issuetype", "summary", "timeoriginalestimate", "description", "status", "key", "self", "labels", "priority", "status" }
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
            var path = string.Format("{0}/issue", Connector.JiraConnector.JiraRestApiUrl);
            var priorityId = !string.IsNullOrEmpty(epic.Priority) ? JiraSettings.GetJiraPriorityIdFromMapping(InstanceUrl, epic.Priority) : string.Empty;
            return _connector.Post<ItemBase>(path, epic.CreateJiraEpic(projectKey, GetProjectMeta().EpicName.Key, priorityId), HttpStatusCode.Created);
        }

        public void DeleteEpicIfExists(string issueKey) // TODO: async
        {
            var existing = GetEpicByKey(issueKey);
            if (existing.HasErrors)
            {
                _log.Error(string.Format("Error attempting to remove jira issue {0}", issueKey));
                _log.Error(string.Format("  message(s) returned : {0}", string.Join(" ||| ", existing.ErrorMessages)));
                return;
            }

            var path = string.Format("{0}/issue/{{issueIdOrKey}}", Connector.JiraConnector.JiraRestApiUrl);
            _connector.Delete(path, HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", issueKey));
        }

        public SearchResult GetStoriesInProject(string jiraProject)
        {
            return GetAllIssuesInProject(jiraProject, "Story");
        }

        public SearchResult GetAllStoriesInProjectSince(string jiraProject, string date)
        {
            return GetAllIssuesInProjectSince(jiraProject, "Story", date);
        }

        public SearchResult GetStoriesWithNoEpicInProject(string projectKey) // TODO: Remove??? This method is only used on a unit test
        {
            return _connector.GetSearchResults(new List<JqOperator>
            {
               JqOperator.Equals("project", projectKey.QuoteReservedWord()),
               JqOperator.Equals("issuetype", "Story"),
               JqOperator.Equals(GetProjectMeta().EpicLink.Property.InQuotes(), JiraAdvancedSearch.Empty)
            },
            new[] { "issuetype", "summary", "description", "priority", "status", "key", "self", "labels", "timetracking", "assignee", GetProjectMeta().StoryPoints.Key },
            (issueKey, fields, properties) =>
            {
                properties.EvalLateBinding(issueKey, GetProjectMeta().StoryPoints, value => fields.StoryPoints = value, _log);
            });
        }

        public SearchResult GetBugsInProject(string jiraProject)
        {
            return GetAllIssuesInProject(jiraProject, "Bug");
        }


        public SearchResult GetAllBugsInProjectSince(string jiraProject, string createdDate)
        {
            var operators = new List<JqOperator>
            {
                JqOperator.Equals("project", jiraProject.QuoteReservedWord()),
                JqOperator.Equals("issuetype", "Bug"),
                JqOperator.CreatedOnOrBefore(createdDate)
            };

            return GetIssuesInProject(operators);
        }

        public IEnumerable<Worklog> GetIssueWorkLogs(string issueKey)
        {
            var path = string.Format("{0}/issue/{{issueIdOrKey}}/worklog", Connector.JiraConnector.JiraRestApiUrl);
            var content = _connector.Get(path, new KeyValuePair<string, string>("issueIdOrKey", issueKey));
            dynamic data = JObject.Parse(content);
            return ((JArray)data.worklogs).Select<dynamic, Worklog>(i => new Worklog
            {
                self = i.self,
                author = new User
                {
                    self = i.author.self,
                    name = i.author.name,
                    key = i.author.key,
                    emailAddress = i.author.emailAddress,
                    displayName = i.author.displayName,
                    active = i.author.active,
                    timeZone = i.author.timeZone
                },
                updateAuthor = new User
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

        public string GetIssueTransitionId(string issueKey, string toState)
        {
            var path = string.Format("{0}/issue/{{issueIdOrKey}}/transitions", Connector.JiraConnector.JiraRestApiUrl);
            var content = _connector.Get(path, new KeyValuePair<string, string>("issueIdOrKey", issueKey));

            dynamic data = JObject.Parse(content);
            var id = ((JArray)data.transitions).Where<dynamic>(i => i.to.name.ToString().ToLower().Equals(toState.ToLower())).Select(i => i.id).FirstOrDefault();

            return id;
        }

        public void RunTransitionOnIssue(string transitionId, string issueKey)
        {
            var path = string.Format("{0}/issue/{{issueIdOrKey}}/transitions", Connector.JiraConnector.JiraRestApiUrl);
            _connector.Post(path, new {transition = new {id = transitionId}}, HttpStatusCode.NoContent,
                new KeyValuePair<string, string>("issueIdOrKey", issueKey));
        }

        public void CleanUpAfterRun(ILog log)
        {
            _projectMeta = null;
        }

        #region PRIVATE METHODS

        private MetaProject GetProjectMeta()
        {
            if (_projectMeta == null)
            {
                var createMeta = _connector.GetCreateMetaInfoForProjects(new List<string> { JiraProject });
                _projectMeta = createMeta.Projects.Single(p => p.Key == JiraProject);
            }

            return _projectMeta;
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

        private SearchResult GetAllIssuesInProjectSince(string jiraProject, string issueType, string dateSince)
        {
            return GetIssuesInProject(new List<JqOperator>()
            {
                JqOperator.Equals("project", jiraProject.QuoteReservedWord()),
                JqOperator.Equals("issuetype", issueType),
                JqOperator.CreatedOnOrBefore(dateSince)
            });
        }

        private SearchResult GetIssuesInProject(IList<JqOperator> operators)
        {
            return _connector.GetSearchResults(operators,
                new[]
                {
                    "issuetype", "summary", "description", "priority", "status", "key", "self", "labels", "timetracking", "assignee",
                    GetProjectMeta().StoryPoints.Key, GetProjectMeta().EpicLink.Key//, GetProjectMeta().Sprint != null ? GetProjectMeta().Sprint.Key : null D-09877
                },
                (issueKey, fields, properties) =>
                {
                    properties.EvalLateBinding(issueKey, GetProjectMeta().StoryPoints, value => fields.StoryPoints = value, _log);
                    properties.EvalLateBinding(issueKey, GetProjectMeta().EpicLink, value => fields.EpicLink = value, _log);
                    // D-09877
                    //if (GetProjectMeta().Sprint != null)
                    //    properties.EvalLateBinding(issueKey, GetProjectMeta().Sprint, value => fields.Sprints = GetSprintsFromSearchResult(value), _log);
                });
        }

        private SearchResult GetAllIssuesInProject(string jiraProject, string issueType)
        {
            return GetIssuesInProject(new List<JqOperator>
            {
                JqOperator.Equals("project", jiraProject.QuoteReservedWord()),
                JqOperator.Equals("issuetype", issueType),
            });
        }

        // D-09877
        //private IEnumerable<Sprint> GetSprintsFromSearchResult(string encodedSprints)
        //{
        //    foreach (var encodedSprint in JArray.Parse(encodedSprints).Values<string>())
        //    {
        //        var propsStartIndex = encodedSprint.IndexOf('[') + 1;
        //        var sprint = encodedSprint.Substring(propsStartIndex, (encodedSprint.Length - 1) - propsStartIndex);
        //        var props = sprint.Split(',').Select(item => item.Split('=')).ToDictionary(pair => pair[0], pair => pair[1]);
        //        yield return new Sprint
        //        {
        //            id = Convert.ToInt32(props["id"]),
        //            rapidViewId = Convert.ToInt32(props["rapidViewId"]),
        //            state = props["state"],
        //            name = props["name"],
        //            startDate = props["startDate"] != "<null>" ? DateTime.Parse(props["startDate"]) : default(DateTime?),
        //            completeDate = props["completeDate"] != "<null>" ? DateTime.Parse(props["completeDate"]) : default(DateTime?),
        //            sequence = Convert.ToInt32(props["sequence"])
        //        };
        //    }
        //}

        #endregion
    }
}