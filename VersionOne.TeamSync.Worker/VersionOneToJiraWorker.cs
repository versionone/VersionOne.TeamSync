using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker
{
    public class VersionOneToJiraWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(VersionOneToJiraWorker));
        private readonly IV1 _v1;
        private readonly IList<IJira> _jiraInstances;
        private readonly List<IAsyncWorker> _asyncWorkers;

        public VersionOneToJiraWorker()
        {
            _v1 = new V1();

            _jiraInstances = new List<IJira>();
            foreach (var serverSettings in JiraSettings.GetInstance().Servers.Cast<JiraServer>().Where(s => s.Enabled))
            {
                var connector = new JiraConnector.Connector.JiraConnector(serverSettings);

                var projectMappings = serverSettings.ProjectMappings.Cast<ProjectMapping>().Where(p => p.Enabled && !string.IsNullOrEmpty(p.JiraProject) && !string.IsNullOrEmpty(p.V1Project) && !string.IsNullOrEmpty(p.EpicSyncType)).ToList();
                if (projectMappings.Any())
                {
                    projectMappings.ForEach(pm => _jiraInstances.Add(new Jira(connector, pm)));
                }
                else
                    Log.ErrorFormat("Jira server '{0}' requires that project mappings are set in the configuration file.", serverSettings.Name);
            }

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
            var syncTime = DateTime.Now;
            Log.Info("Beginning sync...");

            _jiraInstances.ToList().ForEach(jiraInstance =>
            {
                Log.Info(string.Format("Syncing between {0} and {1}", jiraInstance.JiraProject, jiraInstance.V1Project));

                _asyncWorkers.ForEach(worker => worker.DoWork(jiraInstance));

                jiraInstance.CleanUpAfterRun(Log);
            });

            Log.Info("Ending sync...");
            Log.DebugFormat("Total sync time: {0}", DateTime.Now - syncTime);
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
                    Log.DebugFormat("URL: {0}", jiraInstanceInfo.InstanceUrl);
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
                var isMappingValid = true;
                Log.InfoFormat("Verifying V1ProjectID={1} to JiraProjectID={0} project mapping...", jiraInstance.JiraProject, jiraInstance.V1Project);

                if (!jiraInstance.ValidateProjectExists())
                {
                    Log.ErrorFormat("Jira project '{0}' does not exist. Current project mapping will be ignored", jiraInstance.JiraProject);
                    isMappingValid = false;
                }
                if (!_v1.ValidateProjectExists(jiraInstance.V1Project))
                {
                    Log.ErrorFormat(
                        "VersionOne project '{0}' does not exist or does not have a role assigned for user {1}. Current project mapping will be ignored",
                        jiraInstance.V1Project, V1Settings.Settings.Username);
                    isMappingValid = false;
                }
                if (!_v1.ValidateEpicCategoryExists(jiraInstance.EpicCategory))
                {
                    Log.ErrorFormat("VersionOne Epic Category '{0}' does not exist. Current project mapping will be ignored", jiraInstance.EpicCategory);
                    isMappingValid = false;
                }

                if (isMappingValid)
                {
                    Log.Info("Mapping successful! Projects will be synchronized.");
                }
                else
                {
                    Log.Error("Mapping failed. Projects will not be synchronized.");
                    _jiraInstances.Remove(jiraInstance);
                }
            }

            if (!_jiraInstances.Any())
                throw new Exception("No valid projects to synchronize. You need at least one valid project mapping for the service to run.");
        }

        public void ValidateMemberAccountPermissions()
        {
            Log.Info("Verifying VersionOne member account permissions...");
            Log.DebugFormat("Member: {0}", _v1.MemberId);
            if (_v1.ValidateMemberPermissions())
            {
                Log.Info("VersionOne member account has valid permissions.");
            }
            else
            {
                Log.Error("VersionOne member account is not valid, default role must be Project Lead or higher.");
                throw new Exception(string.Format("Unable to validate permissions for {0}.", _v1.MemberId));
            }

            foreach (var jiraInstanceInfo in _jiraInstances.ToList())
            {
                Log.InfoFormat("Verifying JIRA member account permissions...");
                Log.DebugFormat("Server: {0}, User: {1}", jiraInstanceInfo.InstanceUrl, jiraInstanceInfo.Username);
                if (jiraInstanceInfo.ValidateMemberPermissions())
                {
                    Log.Info("JIRA user has valid permissions.");
                }
                else
                {
                    Log.Error("JIRA user is not valid, must belong to 'jira-developers' or 'jira-administrators' group.");
                    throw new Exception(string.Format("Unable to validate permissions for user {0}.", jiraInstanceInfo.Username));
                }
            }
        }

        public void ValidateVersionOneSchedules()
        {
            foreach (var jiraInstance in _jiraInstances.ToList())
            {
                Log.InfoFormat("Validating iteration schedule for {0}.", jiraInstance.V1Project);

                if (_v1.ValidateScheduleExists(jiraInstance.V1Project))
                {
                    Log.DebugFormat("Schedule found!");
                }
                else
                {
                    var result = _v1.CreateScheduleForProject(jiraInstance.V1Project).Result;
                    if (result.Root != null && !result.Root.Name.LocalName.Equals("Error"))
                    {
                        var id = result.Root.Attribute("id").Value;
                        var scheduleId = id.Substring(0, id.LastIndexOf(':')); // OID without snapshot ID
                        Log.DebugFormat("Created schedule {0} for project {1}.", scheduleId, jiraInstance.V1Project);


                        result = _v1.SetScheduleToProject(jiraInstance.V1Project, scheduleId).Result;
                        if (result.Root != null && !result.Root.Name.LocalName.Equals("Error"))
                        {
                            Log.DebugFormat("Schedule {0} is now set to project {1}", scheduleId, jiraInstance.V1Project);
                        }
                        else
                        {
                            LogVersionOneErrorMessage(result);
                            Log.WarnFormat("Unable to set schedule {0} to project {1}.", scheduleId, jiraInstance.V1Project);
                            _jiraInstances.Remove(jiraInstance);
                        }
                    }
                    else
                    {
                        LogVersionOneErrorMessage(result);
                        Log.WarnFormat("Unable to create schedule for {0}, project will not be synchronized.", jiraInstance.V1Project);
                        _jiraInstances.Remove(jiraInstance);
                    }
                }
            }

            if (!_jiraInstances.Any())
                throw new Exception("No valid projects to synchronize. You need at least one VersionOne project with a valid schedule for the service to run.");
        }

        public void ValidatePriorityMappings()
        {
            foreach (var serverSettings in JiraSettings.GetInstance().Servers.Cast<JiraServer>().Where(s => s.Enabled))
            {
                var jira = _jiraInstances.SingleOrDefault(j => j.InstanceUrl.Equals(serverSettings.Url));
                if (jira != null)
                {
                    var jiraDefaultPriorityId = jira.GetPriorityId(serverSettings.PriorityMappings.DefaultJiraPriority);
                    if (jiraDefaultPriorityId == null)
                    {
                        Log.DebugFormat(
                            "Jira default priority '{0}' not found on Jira Server '{1}'. Server won't be synced",
                            serverSettings.PriorityMappings.DefaultJiraPriority, serverSettings.Url);
                        _jiraInstances.Where(j => j.InstanceUrl.Equals(serverSettings.Url))
                            .ToList()
                            .ForEach(j => _jiraInstances.Remove(j));
                    }
                    else
                    {
                        serverSettings.PriorityMappings.DefaultJiraPriorityId = jiraDefaultPriorityId;
                        foreach (var priorityMapping in serverSettings.PriorityMappings.Cast<PriorityMapping>())
                        {
                            var v1WorkitemPriorityId = _v1.GetPriorityId("WorkitemPriority", priorityMapping.V1Priority).Result;
                            priorityMapping.V1WorkitemPriorityId = v1WorkitemPriorityId;
                            if (v1WorkitemPriorityId == null)
                                Log.DebugFormat("Version One workintem priority '{0}' not found. Default priority will be set", priorityMapping.V1Priority);

                            var jiraIssuePriorityId = jira.GetPriorityId(priorityMapping.JiraPriority);
                            priorityMapping.JiraIssuePriorityId = jiraIssuePriorityId;
                            if (jiraIssuePriorityId == null)
                                Log.DebugFormat("Jira priority '{0}' not found. No priority will be set", priorityMapping.JiraPriority);
                        }
                    }
                }
            }
        }

        private void LogVersionOneErrorMessage(XDocument error)
        {
            if (error.Root != null)
            {
                var exceptionNode = error.Root.Element("Exception");
                if (exceptionNode != null)
                {
                    var messageNode = exceptionNode.Element("Message");
                    if (messageNode != null)
                    {
                        Log.Error(messageNode.Value);
                    }
                }
            }
        }
    }
}