using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using JiraSoapProxy;
using log4net;
using log4net.Core;
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

            Console.WriteLine("Number of Worlogs for bug");
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

                    var v1ProjectId = CreateV1Project(projectName, numberOfEpics);

                    _jiraRestService = new JiraRestService(jiraServerSettings);
                    var jiraProjectId = CreateJiraProject(_jiraProxy.login(jiraServerSettings.Username, jiraServerSettings.Password),
                        projectName);

                    CreateStoriesInProject(jiraProjectKey, numberOfStories);
                    CreateBugsInProject(jiraProjectKey, numberOfStories);

                    _projectMappings.Add(v1ProjectId, jiraProjectKey);
                    //CreateBugsInProject(jiraProjectKey, numberOfBugs, numberOfWorklogs);

                    //_projectMappings.Add(v1ProjectId, jiraProjectKey);
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

            Console.ReadKey();
        }

        private static void CreateV1Connector()
        {
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

        private static string CreateJiraProject(string jiraToken, string projectName)
        {
            Console.WriteLine("Creating Jira Project: " + projectName + "...");
            var lastNchars = projectName.Substring(projectName.Length - NumberOfRandomChars);
            var project = _jiraProxy.createProject(jiraToken, "LTP" + lastNchars, projectName, "", "", "admin", null,
                null, null);

            //CreateStoriesInProject(project.id, numberOfStories);

            return project.key;
        }

        private static string CreateV1Project(string projectName, int numberOfEpicsInProject)
        {
            var scope = new Scope() { Name = projectName, Parent = "Scope:0", Scheme = "Scheme:1001" };

            var v1ProjectId = _v1Connector.Post(scope, scope.CreatePayload()).Result.Root.Attribute("id").Value;
            v1ProjectId = v1ProjectId.Substring(0, v1ProjectId.LastIndexOf(':'));

            CreateEpicsInProject(v1ProjectId, numberOfEpicsInProject);

            return v1ProjectId;
        }

        private static void CreateEpicsInProject(string v1ProjectId, int numberOfEpics)
        {
            IJira jiraIntance = getJiraInstance();

            for (int i = 1; i <= numberOfEpics; i++)
            {
                var epicName = string.Format("Epic {0}", i);
                Console.WriteLine("\tCreating V1 Epic " + epicName + "...");
                var epic = new Epic { Name = epicName, ScopeId = v1ProjectId };
                _v1Connector.Post(epic, epic.CreatePayload());
            }
            
            runEpicWorker(jiraIntance);
        }


        private static void runEpicWorker(IJira jiraInstance)
        {
            var epicWorker = new EpicWorker(_v1, Log);
           
            epicWorker.DoWork(jiraInstance);   
        }

        private static IJira getJiraInstance()
        {
            List<IJira> _jiraInstances = new List<IJira>();

            foreach (var serverSettings in JiraSettings.GetInstance().Servers.Cast<JiraServer>().Where(s => s.Enabled))
            {
                var connector = new JiraConnector.Connector.JiraConnector(serverSettings);

                var projectMappings = serverSettings.ProjectMappings.Cast<ProjectMapping>().Where(p => p.Enabled && !string.IsNullOrEmpty(p.JiraProject) && !string.IsNullOrEmpty(p.V1Project) && !string.IsNullOrEmpty(p.EpicSyncType)).ToList();
                if (projectMappings.Any())
                {
                    projectMappings.ForEach(pm => _jiraInstances.Add(new Jira(connector, pm)));
                }
            }

            //_jiraInstances.ToList().ForEach(jiraInstance =>
            //{
            return _jiraInstances.ToArray().First();
            //});
        }


        private static void CreateStoriesInProject(string jiraProjectKey, int numberOfStories)
        {
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
            }
        }

        private static void CreateBugsInProject(string jiraProjectKey, int numberOfBugs, int numberOfWorklogs = 0)
        {
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
                CreateWorkLogs(jiraProjectKey, bugKey, numberOfWorklogs);
            }
        }


        private static void CreateWorkLogs(string jiraProjectKey, string bugKey, int numberOfWorklogs = 0)
        {
            if (numberOfWorklogs>0)
            {
                for (int i = 1; i <= numberOfWorklogs; i++)
                {
                    var newWorklog = new
                            {
                                comment = "I did some work here.",
                                started = "2015-02-15T17:34:37.937-0600",
                                timeSpent = "1h 20m"
                            };
                    _jiraRestService.Post("/api/2/issue/" + bugKey + "/worklog", newWorklog);
                }
             }
        }
        //"2012-02-15T17:34:37.937-0600"
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
