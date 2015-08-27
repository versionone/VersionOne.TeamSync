﻿using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.V1Connector;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker
{
    public class VersionOneToJiraWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(VersionOneToJiraWorker));
        private readonly List<IAsyncWorker> _asyncWorkers;
        private readonly IEnumerable<V1JiraInfo> _jiraInstances;
        private readonly IV1 _v1;
        private static DateTime _syncTime;

        public bool IsActualWorkEnabled { get; private set; }

        public VersionOneToJiraWorker(TimeSpan serviceDuration)
        {
            var anonymousConnector = V1Connector.V1Connector.WithInstanceUrl(V1Settings.Settings.Url)
                .WithUserAgentHeader(Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString());

            ICanSetProxyOrGetConnector authConnector;
            switch (V1Settings.Settings.AuthenticationType)
            {
                case 0:
                    authConnector = (ICanSetProxyOrGetConnector)anonymousConnector
                        .WithAccessToken(V1Settings.Settings.AccessToken);
                    break;
                case 1:
                    authConnector = (ICanSetProxyOrGetConnector)anonymousConnector
                        .WithUsernameAndPassword(V1Settings.Settings.Username, V1Settings.Settings.Password);
                    break;
                case 2:
                    authConnector = anonymousConnector
                        .WithWindowsIntegrated()
                        .UseOAuthEndpoints();
                    break;
                case 3:
                    authConnector = anonymousConnector
                        .WithWindowsIntegrated(V1Settings.Settings.Username, V1Settings.Settings.Password)
                        .UseOAuthEndpoints();
                    break;
                case 4:
                    authConnector = anonymousConnector
                        .WithAccessToken(V1Settings.Settings.AccessToken)
                        .UseOAuthEndpoints();
                    break;

                default:
                    throw new Exception("Unsupported authentication type. Please check the VersionOne authenticationType setting in the config file.");
            }

            if (V1Settings.Settings.Proxy.Enabled)
            {
                authConnector = (ICanSetProxyOrGetConnector)authConnector.WithProxy(new ProxyProvider(new Uri(V1Settings.Settings.Proxy.Url),
                    V1Settings.Settings.Proxy.Username, V1Settings.Settings.Proxy.Password,
                    V1Settings.Settings.Proxy.Domain));
            }

            _v1 = new V1(authConnector.Build(), serviceDuration);

            _asyncWorkers = new List<IAsyncWorker>
            {
                new EpicWorker(_v1, Log),
                new StoryWorker(_v1, Log),
                new DefectWorker(_v1, Log),
                new ActualsWorker(_v1, Log)
            };

            _jiraInstances = V1JiraInfo.BuildJiraInfo(JiraSettings.Settings.Servers);
        }


        public VersionOneToJiraWorker(IV1 v1)
        {
            _v1 = v1;
            _asyncWorkers = new List<IAsyncWorker>
            {
                new EpicWorker(_v1, Log),
                new StoryWorker(_v1, Log),
                new DefectWorker(_v1, Log),
                new ActualsWorker(_v1, Log)
            };
        }

        public void DoWork()
        {
            Log.Info("Beginning sync...");

            _jiraInstances.ToList().ForEach(jiraInfo =>
            {
                _syncTime = DateTime.Now;
                Log.Info(string.Format("Syncing between {0} and {1}", jiraInfo.JiraKey, jiraInfo.V1ProjectId));

                _asyncWorkers.ForEach(worker => worker.DoWork(jiraInfo));

                jiraInfo.JiraInstance.CleanUpAfterRun(Log);
            });

            Log.Info("Ending sync...");
            Log.DebugFormat("Total sync time: {0}", DateTime.Now - _syncTime);
        }

        public void ValidateConnections()
        {
            if (_jiraInstances.Any())
            {
                Log.Info("Verifying VersionOne connection...");
                Log.DebugFormat("URL: {0}", _v1.InstanceUrl);
                if (_v1.ValidateConnection())
                {
                    Log.Info("VersionOne connection successful!");
                }
                else
                {
                    Log.Error("VersionOne connection failed.");
                    throw new Exception(string.Format("Unable to validate connection to {0}.", _v1.InstanceUrl));
                }

                foreach (var jiraInstanceInfo in _jiraInstances.ToList())
                {
                    Log.InfoFormat("Verifying Jira connection...");
                    Log.DebugFormat("URL: {0}", jiraInstanceInfo.JiraInstance.InstanceUrl);
                    Log.Info(jiraInstanceInfo.ValidateConnection()
                        ? "Jira connection successful!"
                        : "Jira connection failed!");
                }
            }
            else
            {
                throw new Exception("The service requires at least one valid Jira server with one valid project mapping to run.");
            }
        }

        public void ValidateProjectMappings()
        {
            foreach (var jiraInstance in _jiraInstances.ToList())
            {
                Log.InfoFormat("Verifying V1ProjectID={1} to JiraProjectID={0} project mapping...", jiraInstance.JiraKey, jiraInstance.V1ProjectId);

                if (jiraInstance.ValidateMapping(_v1))
                {
                    Log.Info("Mapping successful! Projects will be synchronized.");
                }
                else
                {
                    Log.Error("Mapping failed. Projects will not be synchronized.");
                    ((HashSet<V1JiraInfo>)_jiraInstances).Remove(jiraInstance);
                }
            }

            if (!_jiraInstances.Any())
                throw new Exception("No valid projects to synchronize. You need at least one valid project mapping for the service to run.");
        }

        public void ValidateVersionOneSchedules()
        {
            foreach (var jiraInstance in _jiraInstances.ToList())
            {
                Log.InfoFormat("Validating iteration schedule for {0}.", jiraInstance.V1ProjectId);

                if (_v1.ValidateScheduleExists(jiraInstance.V1ProjectId))
                {
                    Log.DebugFormat("Schedule found!");
                }
                else 
                {
                    var result = _v1.CreateSchedule().Result;
                    if (!result.Root.Name.LocalName.Equals("Error"))
                        Log.DebugFormat("Created schedule: {0}", result);
                    else
                    {
                        LogVersionOneErrorMessage(result);
                        throw new Exception("Error occurred while creating the schedule. Service will now be stopped.");
                    }

                    var id = result.Root.Attribute("id").Value;
                    var scheduleId = id.Substring(0, id.LastIndexOf(':'));
                    result = _v1.SetScheduleToProject(jiraInstance.V1ProjectId, scheduleId).Result;
                    if (!result.Root.Name.LocalName.Equals("Error"))
                        Log.DebugFormat("Set schedule for {0}", jiraInstance.V1ProjectId);
                    else
                    {
                        LogVersionOneErrorMessage(result);
                        throw new Exception(
                            string.Format(
                                "Error occurred while setting schedule {0} to project {1}. Service will now be stopped.",
                                scheduleId, jiraInstance.V1ProjectId));
                    }
                }
            }
        }

        private static void LogVersionOneErrorMessage(XDocument error)
        {
            if (error.Root != null)
            {
                var exceptionNode = error.Root.Element("Exception");
                if (exceptionNode != null)
                {
                    var messageNode = exceptionNode.Element("Message");
                    if (messageNode != null)
                        Log.Error(messageNode.Value);
                }
            }
        }
    }
}
