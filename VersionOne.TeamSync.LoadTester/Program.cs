﻿using System;
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
        private const int NumberOfV1Epics = 0;
        private const int NumberOfBugs = 0;
        private const int NumberOfStories = 0;
        private const int NumberOfWorlogs = 5;
        static Epic[] v1Epics = new Epic[] { };
        private const int NumberOfRandomChars = 3;

        private static Random _random = new Random((int)DateTime.Now.Ticks);
        private static IV1Connector _v1Connector;
        private static JiraSoapService _jiraProxy;
        private static Dictionary<string, string> _projectMappings = new  Dictionary<string, string>();

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

                    var jiraProjectId = CreateJiraProject(_jiraProxy.login(jiraServerSettings.Username, jiraServerSettings.Password),
                        projectName);

                    _projectMappings.Add(v1ProjectId, jiraProjectId);
                    //createStory(jiraProjectId);
                    //createBug(jiraProjectId);

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

        private static void createBug(string jiraProjectId)
        {

            //createWorlog
            throw new NotImplementedException();
        }

        private static void RunServiceOnce(string jiraProjectId)
        {
            throw new NotImplementedException();
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

            CreateV1Epics(v1ProjectId);

            return v1ProjectId;
        }

        private static void CreateV1Epics(string v1ProjectId)
        {
          
            for (int i = 1; i <= NumberOfV1Epics; i++)
            {
                var epicName = AddRandomCharsToName("Load Testing Epic ") + " on " + v1ProjectId;
                Console.WriteLine("\tCreating V1 Epic " + epicName + "...");
                var epic = new Epic() {Name = epicName, ScopeId = v1ProjectId};
                _v1Connector.Post(epic, epic.CreatePayload());
                createStory(epic);
                createDefect(epic);
            }
        }

        private static void createStory(Epic epic)
        {
            for (int i = 1; i <= NumberOfV1Epics; i++)
            {
                
            }
        }

        private static void createDefect(Epic epic)
        {

            for (int i = 1; i <= NumberOfV1Epics; i++)
            {

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
