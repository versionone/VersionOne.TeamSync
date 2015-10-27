using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using JiraSoapProxy;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.LoadTester
{
    class Program
    {
        private const int NumberOfProjects = 2;
     
        static Epic[] v1Epics = new Epic[] { };
        private const int NumberOfRandomChars = 3;

        private static Random _random = new Random((int)DateTime.Now.Ticks);
        private static IV1Connector _v1Connector;
        private static JiraSoapService _jiraProxy;
        private static Dictionary<string, string> _projectMappings = new  Dictionary<string, string>();

        static void Main(string[] args)
        {
            var serviceConfigFilePath = "C:\\dev\\repos\\VersionOne.TeamSync\\VersionOne.TeamSync.Service\\App.config";
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

            //foreach (var projectMapping in _projectMappings)
            //{
            //    CreateStory(projectMapping.Key.ToString(),20);
            //    CreateDefect(projectMapping.Key.ToString(), 20);
            //}
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
            var project = _jiraProxy.createProject(jiraToken, "LTP" + lastNchars, projectName, "", "", "admin", null, null,
                null);

            return project.key;
        }


        private static string CreateV1Project(string projectName)
        {
            var scope = new Scope(){Name = projectName, Parent = "Scope:0", Scheme = "Scheme:1001"};

            var v1ProjectId = _v1Connector.Post(scope, scope.CreatePayload()).Result.Root.Attribute("id").Value;
            v1ProjectId = v1ProjectId.Substring(0, v1ProjectId.LastIndexOf(':'));

            CreateV1Epics(v1ProjectId, 20);

            return v1ProjectId;
        }

        private static void CreateV1Epics(string v1ProjectId, int numberOfV1Epics)
        {
            for (int i = 1; i <= numberOfV1Epics; i++)
            {
                var epicName = AddRandomCharsToName("Load Testing Epic ") + " on " + v1ProjectId;
                Console.WriteLine("\tCreating V1 Epic " + epicName + "...");
                var epic = new Epic() {Name = epicName, ScopeId = v1ProjectId};
                _v1Connector.Post(epic, epic.CreatePayload());
                
                //CreateStory(epic, 5);
                //CreateDefect(epic, 5);
            }
        }

        private static void CreateStory(Epic epic, int numbersOfStories)
        {
            for (int i = 1; i <= numbersOfStories; i++)
            {
                var story = new Story() { Name = "LoadTestStory"+i.ToString()};
                _v1Connector.Post(story,story.CreatePayload());
            }
        }

        private static void CreateDefect(Epic epic, int numberOfDefects)
        {

            for (int i = 1; i <= numberOfDefects; i++)
            {
                var defect = new Defect { Name = "LoadTest-"+i.ToString(), Number = i.ToString(), Estimate = "", ToDo = "", Description = "Load TesT " };
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
