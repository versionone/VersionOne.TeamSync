using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using JiraSoapProxy;
using log4net;
using log4net.Core;
using Newtonsoft.Json.Linq;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.LoadTester
{
    class Program
    {
        private const int NumberOfRandomChars = 3;

        private static Random _random = new Random((int)DateTime.Now.Ticks);
        private static IV1Connector _v1Connector;
        private static JiraSoapService _jiraProxy;
        private static JiraRestService _jiraRestService;
        private static Dictionary<string, string> _projectMappings = new Dictionary<string, string>();

        private static String _serviceConfigFilePath;

        private static V1 _v1 = new V1();
        private static ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            //TODO: validate args
            _serviceConfigFilePath = args[0];
            if (string.IsNullOrWhiteSpace(_serviceConfigFilePath))
                throw new ArgumentNullException("_serviceConfigFilePath");

            Console.WriteLine("Number of Epics:");
            var n1 = Console.ReadLine();
            var numberOfEpics = Convert.ToInt32(n1);

            Console.WriteLine("Number of Stories per Epic:");
            var n2 = Console.ReadLine();
            var numberOfStoriesPerEpic = Convert.ToInt32(n2);

            Console.WriteLine("Number of Bugs per Epic:");
            var n3 = Console.ReadLine();
            var numberOfBugsPerEpic = Convert.ToInt32(n3);

            Console.WriteLine("Number of Stories with no linked Epic:");
            var n4 = Console.ReadLine();
            var numberOfStories = Convert.ToInt32(n4);

            Console.WriteLine("Number of Bugs with no linked Epic:");
            var n5 = Console.ReadLine();
            var numberOfBugs = Convert.ToInt32(n5);

            Console.WriteLine("Number of Worlogs:");
            var n6 = Console.ReadLine();
            var numberOfWorklogs = Convert.ToInt32(n6);

            Console.WriteLine("Jira project keys:");
            var jiraProjectKeys = Console.ReadLine().Split(',').Select(x => x.ToUpper());

            CreateV1Connector();
            _jiraProxy = new JiraSoapService();

            var config =
                ConfigurationManager.OpenMappedMachineConfiguration(new ConfigurationFileMap(_serviceConfigFilePath));
            var jiraSettings = config.GetSection("jiraSettings") as JiraSettings;
            if (jiraSettings != null)
            {
                var jiraServerSettings = jiraSettings.Servers[0];
                foreach (var jiraProjectKey in jiraProjectKeys)
                {
                    var projectName = string.Format("Load Testing project {0}", jiraProjectKey);
                    Console.WriteLine("Creating V1 Project: " + projectName + "...");

                    var v1ProjectId = CreateV1Project(projectName, numberOfEpics, jiraProjectKey);

                    _jiraRestService = new JiraRestService(jiraServerSettings);

                    //var jiraProjectId = CreateJiraProject(_jiraProxy.login(jiraServerSettings.Username, jiraServerSettings.Password),
                    //    projectName);

                    SyncEpics(v1ProjectId, jiraProjectKey, numberOfStoriesPerEpic, numberOfBugsPerEpic);

                    CreateStoriesInProject(jiraProjectKey, numberOfStories, numberOfWorklogs);
                    CreateBugsInProject(jiraProjectKey, numberOfBugs, numberOfWorklogs);

                    _projectMappings.Add(v1ProjectId, jiraProjectKey);
                }

                foreach (var projectMapping in _projectMappings)
                {
                    jiraServerSettings.ProjectMappings.Add(new ProjectMapping
                    {
                        Enabled = true,
                        V1Project = projectMapping.Key,
                        JiraProject = projectMapping.Value,
                        EpicSyncType = "EpicCategory:208"
                    });
                }
            }

            config.Save(ConfigurationSaveMode.Full);
            Console.WriteLine("Finish. press any key.");
            Console.ReadKey();
        }

        private static void CreateV1Connector()
        {
            Console.WriteLine("Creating V1 Connector...");
            switch (V1Settings.Settings.AuthenticationType)
            {
                case 0:
                    _v1Connector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithAccessToken(V1Settings.Settings.AccessToken)
                        .Build();
                    break;
                case 1:
                    _v1Connector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                        .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString())
                        .WithUsernameAndPassword(V1Settings.Settings.Username, V1Settings.Settings.Password)
                        .Build();
                    break;
                default:
                    throw new Exception("Unsupported authentication type. Please check the VersionOne authenticationType setting in the config file.");
            }
        }

        //private static string CreateJiraProject(string jiraToken, string projectName)
        //{
        //    Console.WriteLine("Creating Jira Project: " + projectName + "...");
        //    var lastNchars = projectName.Substring(projectName.Length - NumberOfRandomChars);
        //    var project = _jiraProxy.createProject(jiraToken, "LTP" + lastNchars, projectName, "", "", "admin", null,
        //        null, null);

        //    //CreateStoriesInProject(project.id, numberOfStories);

        //    return project.key;
        //}

        private static string CreateV1Project(string projectName, int numberOfEpicsInProject, string jiraProjectKey)
        {
            var scope = new Scope() { Name = projectName, Parent = "Scope:0", Scheme = "Scheme:1001" };
            
            var v1ProjectId = _v1Connector.Post(scope, scope.CreatePayload()).Result.Root.Attribute("id").Value;
            v1ProjectId = v1ProjectId.Substring(0, v1ProjectId.LastIndexOf(':'));

            CreateEpicsInProject(v1ProjectId, numberOfEpicsInProject);

            return v1ProjectId;
        }

        private static void CreateEpicsInProject(string v1ProjectId, int numberOfEpics)
        {
            Console.WriteLine("Creating Epics...");
            for (int i = 1; i <= numberOfEpics; i++)
            {
                var epicName = string.Format("Epic {0}", i);
                Console.WriteLine("\tCreating V1 Epic " + epicName + "...");
                var epic = new Epic { Name = epicName, ScopeId = v1ProjectId };
                var payload = epic.CreatePayload();
                payload.AddSetRelationNode("Category", "EpicCategory:208");

                _v1Connector.Post(epic, payload);
                Console.WriteLine("Created "+ epicName );
            }
        }

        private static void SyncEpics(string v1ProjectId, string jiraProjectKey, int numberOfStoriesPerEpic, int numberOfBugsPerEpic)
        {
            Console.WriteLine("Synchronizing Epics...");
            var metaStr = _jiraRestService.Get("api/2/issue/createmeta", new Dictionary<string, string>
            {
                {"projectKeys", string.Join(",", jiraProjectKey)},
                {"issuetypeNames", "Epic"},
                {"expand", "projects.issuetypes.fields"}
            });
            dynamic meta = JObject.Parse(metaStr);
            JObject dict =
                ((JArray)((JArray)meta.projects).First<dynamic>().issuetypes).First<dynamic>()
                    .fields;

            var epicNameFieldName = "";
            var epicLinkFieldName = "";
            foreach (var property in dict.Properties())
            {
                if (property.Value.ToString().Contains("Epic Name"))
                {
                    epicNameFieldName = property.Name;
                }
                if (property.Value.ToString().Contains("Epic Link"))
                {
                    epicLinkFieldName = property.Name;
                }
            }

            var epics = GetEpicsWithoutReference(v1ProjectId, "EpicCategory:208").Result;
            foreach (var epic in epics)
            {
                dynamic jiraEpic = new ExpandoObject();
                jiraEpic.fields = new Dictionary<string, object>
                {
                    {"description", epic.Description ?? "-"},
                    {"summary", epic.Name},
                    {"issuetype", new {name = "Epic"}},
                    {"project", new {key = jiraProjectKey}},
                    {epicNameFieldName, epic.Name},
                    {"labels", new List<string> {epic.Number}}
                };

                var epicKey = _jiraRestService.Post("api/2/issue", jiraEpic);
                Console.WriteLine("Saved Epics without link...");

                for (int i = 1; i <= numberOfStoriesPerEpic; i++)
                {
                    dynamic linkedStory = new ExpandoObject();
                    linkedStory.fields = new Dictionary<string, object>
                    {
                        {"project", new {key = jiraProjectKey}},
                        {"summary", string.Format("Story {0}", i)},
                        {"issuetype", new {name = "Story"}},
                        {"description", string.Format("Story linked to {0}", epic.Name)},
                        {epicLinkFieldName, epicKey},
                        {"labels", new List<string> {epic.Number}}
                    };

                    _jiraRestService.Post("api/2/issue", linkedStory);
                    Console.WriteLine("Saved Stories linked...");
                }

                for (int i = 1; i <= numberOfBugsPerEpic; i++)
                {
                    dynamic linkedBug = new ExpandoObject();
                    linkedBug.fields = new Dictionary<string, object>
                    {
                        {"project", new {key = jiraProjectKey}},
                        {"summary", string.Format("Bug {0}", i)},
                        {"issuetype", new {name = "Bug"}},
                        {"description", string.Format("Bug linked to {0}", epic.Name)},
                        {epicLinkFieldName, epicKey},
                        {"labels", new List<string> {epic.Number}}
                    };

                    _jiraRestService.Post("api/2/issue", linkedBug);
                    Console.WriteLine("Saved Bugs linked ...");
                }
            }
        }

        private static void CreateStoriesInProject(string jiraProjectKey, int numberOfStories, int numberOfWorklogs)
        {
            Console.WriteLine("Creating Stories with no linked Epic...");
            for (int i = 1; i <= numberOfStories; i++)
            {
                var newStory = new
                {
                    fields = new
                    {
                        project = new { key = jiraProjectKey },
                        summary = string.Format("Story {0}", i),
                        description = "Story with no linked Epic",
                        issuetype = new { name = "Story" }
                    }
                };

                var storyKey = _jiraRestService.Post("api/2/issue", newStory);
                
                CreateWorkLogs(storyKey, numberOfWorklogs);
            }
        }
        
        private static void CreateBugsInProject(string jiraProjectKey, int numberOfBugs, int numberOfWorklogs)
        {
            Console.WriteLine("Creating Bug with no linked Epic...");
            for (int i = 1; i <= numberOfBugs; i++)
            {
                var newBug = new
                {
                    fields = new
                    {
                        project = new { key = jiraProjectKey },
                        summary = string.Format("Bug {0}", i),
                        description = "Bug with no linked Epic",
                        issuetype = new { name = "Bug" }
                    }
                };

                var bugKey = _jiraRestService.Post("api/2/issue", newBug);
                CreateWorkLogs(bugKey, numberOfWorklogs);
            }
        }

        private static void CreateWorkLogs(string bugKey, int numberOfWorklogs)
        {
            if (numberOfWorklogs > 0)
            {
                Console.WriteLine("Creating Worklogs...");
                
                for (int i = 1; i <= numberOfWorklogs; i++)
                {
                    var newWorklog = new
                            {
                                comment = "Added some Worklog." + i.ToString(),
                                started = "2015-11-15T17:34:37.937-0600",
                                timeSpent = "1h 20m"
                            };
                    _jiraRestService.Post("/api/2/issue/" + bugKey + "/worklog", newWorklog);
                }
            }
        }

        public static Task<List<Epic>> GetEpicsWithoutReference(string v1Project, string epicCategory)
        {
            return _v1Connector.Query("Epic",
                new[] { "ID.Number", "Name", "Description", "Scope.Name", "Priority.Name" },
                new[]
                {
                    "Reference=\"\"",
                    "AssetState='Active'",
                    string.Format("Scope=\"{0}\"", v1Project),
                    string.Format("Category=\"{0}\"", epicCategory)
                }, Epic.FromQuery);
        }

        private static string AddRandomCharsToName(string name)
        {
            StringBuilder builder = new StringBuilder(name);
            char ch;
            for (int i = 0; i < NumberOfRandomChars; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * _random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }
    }

    public class Scope : IV1Asset
    {
        public string AssetType
        {
            get { return "Scope"; }
        }

        public string ID { get; private set; }

        public string Name { get; set; }

        public string Parent { get; set; }

        public string Scheme { get; set; }

        public string Error { get; private set; }

        public bool HasErrors { get; private set; }

        public XDocument CreatePayload()
        {
            var doc = XDocument.Parse("<Asset></Asset>");
            doc.AddSetNode("Name", Name)
                .AddSetNode("BeginDate", DateTime.Now.ToString(CultureInfo.InvariantCulture))
                .AddSetRelationNode("Parent", Parent)
                .AddSetRelationNode("Scheme", Scheme);
            return doc;
        }
    }

    public static class EpicExtensions
    {
        public static XDocument CreatePayload(this Epic epic)
        {
            var doc = XDocument.Parse("<Asset></Asset>");
            doc.AddSetNode("Name", epic.Name)
                .AddSetRelationNode("Scope", epic.ScopeId);
            return doc;
        }
    }
}
