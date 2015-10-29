using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using JiraSoapProxy;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.LoadTester
{
    class Program
    {
        private const int NumberOfProjects = 1;
        private const int NumberOfEpics = 0;
        private const int NumberOfStoriesPerEpic = 5;
        private const int NumberOfStories = 0;

        private const int NumberOfRandomChars = 3;

        private static Random _random = new Random((int)DateTime.Now.Ticks);
        private static IV1Connector _v1Connector;
        private static JiraSoapService _jiraProxy;
        private static JiraRestService _jiraRestService;
        private static Dictionary<string, string> _projectMappings = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            var serviceConfigFilePath = args[0];
            if (string.IsNullOrWhiteSpace(serviceConfigFilePath))
                throw new ArgumentNullException("serviceConfigFilePath");

            CreateV1Connector();
            _jiraProxy = new JiraSoapService();

            var config =
                ConfigurationManager.OpenMappedMachineConfiguration(new ConfigurationFileMap(serviceConfigFilePath));
            var jiraSettings = config.GetSection("jiraSettings") as JiraSettings;
            if (jiraSettings != null)
            {
                var jiraServerSettings = jiraSettings.Servers[0];
                for (int i = 1; i <= NumberOfProjects; i++)
                {
                    var projectName = AddRandomCharsToName("Load Testing project ");
                    Console.WriteLine("Creating V1 Project: " + projectName + "...");

                    var v1ProjectId = CreateV1Project(projectName);

                    _jiraRestService = new JiraRestService(jiraServerSettings);
                    var jiraProjectId = CreateJiraProject(_jiraProxy.login(jiraServerSettings.Username, jiraServerSettings.Password),
                        projectName);

                    _projectMappings.Add(v1ProjectId, jiraProjectId);
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

            CreateStoriesInProject(project.id);

            return project.key;
        }

        private static string CreateV1Project(string projectName)
        {
            var scope = new Scope() { Name = projectName, Parent = "Scope:0", Scheme = "Scheme:1001" };

            var v1ProjectId = _v1Connector.Post(scope, scope.CreatePayload()).Result.Root.Attribute("id").Value;
            v1ProjectId = v1ProjectId.Substring(0, v1ProjectId.LastIndexOf(':'));

            CreateEpicsInProject(v1ProjectId);

            return v1ProjectId;
        }

        private static void CreateEpicsInProject(string v1ProjectId)
        {
            for (int i = 1; i <= NumberOfEpics; i++)
            {
                var epicName = string.Format("Epic {0}", i);
                Console.WriteLine("\tCreating V1 Epic " + epicName + "...");
                var epic = new Epic { Name = epicName, ScopeId = v1ProjectId };
                _v1Connector.Post(epic, epic.CreatePayload());
            }
        }

        private static void CreateStoriesInProject(string jiraProjectId)
        {
            for (int i = 1; i <= NumberOfStories; i++)
            {
                //var newIssue = new RemoteIssue {summary = string.Format("Story {0}", i), project = jiraProjectKey, type = "10001"};
                var newStory = new
                {
                    Fields = new {Project = new {Id = jiraProjectId}},
                    Summary = string.Format("Story {0}", i),
                    IssueType = new {Name = "Story"},
                    Reporter = new {Name = "LoadTester"}
                };

                _jiraRestService.Post("api/2/issue", newStory);
            }
        }

        private static void CreateDefect(int numberOfDefects, Epic epic = null)
        {
            for (int i = 1; i <= numberOfDefects; i++)
            {
                var defect = new Defect { Name = "LoadTest-" + i.ToString(), Number = i.ToString(), Estimate = "", ToDo = "", Description = "Load TesT " };
            }
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
