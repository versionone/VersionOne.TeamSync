using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersionOne.TeamSync.Core.Config;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.VersionOne.Domain;

namespace VersionOne.TeamSync.JiraWorker
{
    public class VersionOneToJiraWorker : IV1StartupWorker
    {
        [Import]
        private readonly IV1 _v1;
        private readonly IList<IJira> _jiraInstances;
        private readonly List<IAsyncWorker> _asyncWorkers;
        private readonly IV1Log _v1Log;

        [ImportingConstructor]
        public VersionOneToJiraWorker([Import]IV1LogFactory v1LogFactory)
        {
            _v1Log = v1LogFactory.Create<VersionOneToJiraWorker>();
            _jiraInstances = new List<IJira>();

            var jiraDate = GetRunFrom();

            foreach (var serverSettings in JiraSettings.GetInstance().Servers.Cast<JiraServer>().Where(s => s.Enabled))
            {
                var connector = new JiraConnector.Connector.JiraConnector(serverSettings);
                
                var projectMappings = serverSettings.ProjectMappings.Cast<ProjectMapping>().Where(p => p.Enabled && !string.IsNullOrEmpty(p.JiraProject) && !string.IsNullOrEmpty(p.V1Project) && !string.IsNullOrEmpty(p.EpicSyncType)).ToList();
                if (projectMappings.Any())
                    projectMappings.ForEach(pm => _jiraInstances.Add(new Jira(connector, v1LogFactory, pm, jiraDate)));
                else
                    _v1Log.ErrorFormat("Jira server '{0}' requires that project mappings are set in the configuration file.", serverSettings.Name);
            }

            _asyncWorkers = new List<IAsyncWorker>
            {
                new EpicWorker(_v1, _v1Log),
                new StoryWorker(_v1, _v1Log),
                new DefectWorker(_v1, _v1Log),
                new ActualsWorker(_v1, _v1Log)
            };
        }

        private string GetRunFrom()
        {
            var runDate = JiraSettings.GetInstance().RunFromThisDateOn;

            DateTime parsedRunFromDate;

            if (string.IsNullOrEmpty(runDate))
            {
                parsedRunFromDate = new DateTime(1980, 1, 1);
                _v1Log.Info("No date found, defaulting to " + parsedRunFromDate.ToString("yyyy-MM-dd"));
            }
            else if (!DateTime.TryParse(runDate, out parsedRunFromDate))
            {
                _v1Log.Error("Invalid date : " + runDate);
                throw new ConfigurationErrorsException("RunFromThisDateOn contains an invalid entry");
            }

            return parsedRunFromDate.ToString("yyyy-MM-dd");
        }

        public void DoFirstRun()
        {
            var syncTime = DateTime.Now;
            _v1Log.Info("Beginning first run...");

            _jiraInstances.ToList().ForEach(jiraInstance => 
            {
                _v1Log.Info(string.Format("Doing first run between {0} and {1}", jiraInstance.JiraProject, jiraInstance.V1Project));
                Task.WaitAll(_asyncWorkers.Select(worker => worker.DoFirstRun(jiraInstance)).ToArray());
                jiraInstance.CleanUpAfterRun(_v1Log);
            });

            _v1Log.Info("Ending first run...");
            _v1Log.DebugFormat("Total time: {0}", DateTime.Now - syncTime);
        }

        public void DoWork()
        {
            var syncTime = DateTime.Now;
            _v1Log.Info("Beginning sync...");

            _jiraInstances.ToList().ForEach(jiraInstance =>
            {
                _v1Log.Info(string.Format("Syncing between {0} and {1}", jiraInstance.JiraProject, jiraInstance.V1Project));

                Task.WaitAll(_asyncWorkers.Select(worker => worker.DoWork(jiraInstance)).ToArray());

                jiraInstance.CleanUpAfterRun(_v1Log);
            });

            _v1Log.Info("Ending sync...");
            _v1Log.DebugFormat("Total sync time: {0}", DateTime.Now - syncTime);
        }

        public bool IsActualWorkEnabled
        {
            get { throw new NotImplementedException(); }
        }

        public void ValidateConnections()
        {
            if (_jiraInstances.Any())
            {
                _v1Log.Info("Verifying VersionOne connection...");
                _v1Log.DebugFormat("URL: {0}", _v1.InstanceUrl);
                if (_v1.ValidateConnection())
                {
                    _v1Log.Info("VersionOne connection successful!");
                }
                else
                {
                    _v1Log.Error("VersionOne connection failed.");
                    throw new Exception(string.Format("Unable to validate connection to {0}.", _v1.InstanceUrl));
                }

                foreach (var jiraInstanceInfo in _jiraInstances.ToList())
                {
                    _v1Log.InfoFormat("Verifying Jira connection...");
                    _v1Log.DebugFormat("URL: {0}", jiraInstanceInfo.InstanceUrl);
                    _v1Log.Info(jiraInstanceInfo.ValidateConnection()
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
                _v1Log.InfoFormat("Verifying V1ProjectID={1} to JiraProjectID={0} project mapping...", jiraInstance.JiraProject, jiraInstance.V1Project);

                if (!jiraInstance.ValidateProjectExists())
                {
                    _v1Log.ErrorFormat("Jira project '{0}' does not exist. Current project mapping will be ignored", jiraInstance.JiraProject);
                    isMappingValid = false;
                }
                if (!_v1.ValidateProjectExists(jiraInstance.V1Project))
                {
                    _v1Log.ErrorFormat(
                        "VersionOne project '{0}' does not exist or does not have a role assigned for user {1}. Current project mapping will be ignored",
                        jiraInstance.V1Project, V1Settings.Settings.Username);
                    isMappingValid = false;
                }
                if (!_v1.ValidateEpicCategoryExists(jiraInstance.EpicCategory))
                {
                    _v1Log.ErrorFormat("VersionOne Epic Category '{0}' does not exist. Current project mapping will be ignored", jiraInstance.EpicCategory);
                    isMappingValid = false;
                }

                if (isMappingValid)
                {
                    _v1Log.Info("Mapping successful! Projects will be synchronized.");
                }
                else
                {
                    _v1Log.Error("Mapping failed. Projects will not be synchronized.");
                    _jiraInstances.Remove(jiraInstance);
                }
            }

            if (!_jiraInstances.Any())
                throw new Exception("No valid projects to synchronize. You need at least one valid project mapping for the service to run.");
        }

        public void ValidateMemberAccountPermissions()
        {
            _v1Log.Info("Verifying VersionOne member account permissions...");
            _v1Log.DebugFormat("Member: {0}", _v1.MemberId);
            if (_v1.ValidateMemberPermissions())
            {
                _v1Log.Info("VersionOne member account has valid permissions.");
            }
            else
            {
                _v1Log.Error("VersionOne member account is not valid, default role must be Project Lead or higher.");
                throw new Exception(string.Format("Unable to validate permissions for {0}.", _v1.MemberId));
            }

            foreach (var jiraInstanceInfo in _jiraInstances.ToList())
            {
                _v1Log.InfoFormat("Verifying JIRA member account permissions...");
                _v1Log.DebugFormat("Server: {0}, User: {1}", jiraInstanceInfo.InstanceUrl, jiraInstanceInfo.Username);
                if (jiraInstanceInfo.ValidateMemberPermissions())
                {
                    _v1Log.Info("JIRA user has valid permissions.");
                }
                else
                {
                    _v1Log.Error("JIRA user is not valid, must belong to 'jira-developers' or 'jira-administrators' group.");
                    throw new Exception(string.Format("Unable to validate permissions for user {0}.", jiraInstanceInfo.Username));
                }
            }
        }

        //public void ValidateVersionOneSchedules()
        //{
        //    foreach (var jiraInstance in _jiraInstances.ToList())
        //    {
        //        _v1Log.InfoFormat("Validating iteration schedule for {0}.", jiraInstance.V1ProjectId);

        //        if (_v1.ValidateScheduleExists(jiraInstance.V1ProjectId))
        //        {
        //            _v1Log.DebugFormat("Schedule found!");
        //        }
        //        else
        //        {
        //            var result = _v1.CreateScheduleForProject(jiraInstance.V1ProjectId).Result;
        //            if (!result.Root.Name.LocalName.Equals("Error"))
        //            {
        //                var id = result.Root.Attribute("id").Value;
        //                var scheduleId = id.Substring(0, id.LastIndexOf(':')); // OID without snapshot ID
        //                _v1Log.DebugFormat("Created schedule {0} for project {1}.", scheduleId, jiraInstance.V1ProjectId);


        //                result = _v1.SetScheduleToProject(jiraInstance.V1ProjectId, scheduleId).Result;
        //                if (!result.Root.Name.LocalName.Equals("Error"))
        //                {
        //                    _v1Log.DebugFormat("Schedule {0} is now set to project {1}", scheduleId, jiraInstance.V1ProjectId);
        //                }
        //                else
        //                {
        //                    LogVersionOneErrorMessage(result);
        //                    _v1Log.WarnFormat("Unable to set schedule {0} to project {1}.", scheduleId, jiraInstance.V1ProjectId);
        //                    ((HashSet<V1JiraInfo>)_jiraInstances).Remove(jiraInstance);
        //                }
        //            }
        //            else
        //            {
        //                LogVersionOneErrorMessage(result);
        //                _v1Log.WarnFormat("Unable to create schedule for {0}, project will not be synchronized.", jiraInstance.V1ProjectId);
        //                ((HashSet<V1JiraInfo>)_jiraInstances).Remove(jiraInstance);
        //            }
        //        }
        //    }

        //    if (!_jiraInstances.Any())
        //        throw new Exception("No valid projects to synchronize. You need at least one VersionOne project with a valid schedule for the service to run.");
        //}

        public void ValidatePriorityMappings()
        {
            foreach (var serverSettings in JiraSettings.GetInstance().Servers.Cast<JiraServer>().Where(s => s.Enabled))
            {
                var jira = _jiraInstances.FirstOrDefault(j => j.InstanceUrl.Equals(serverSettings.Url));
                if (jira != null)
                {
                    var jiraDefaultPriorityId = jira.GetPriorityId(serverSettings.PriorityMappings.DefaultJiraPriority);
                    if (jiraDefaultPriorityId == null)
                    {
                        _v1Log.DebugFormat(
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
                                _v1Log.DebugFormat("Version One workintem priority '{0}' not found. Default priority will be set", priorityMapping.V1Priority);

                            var jiraIssuePriorityId = jira.GetPriorityId(priorityMapping.JiraPriority);
                            priorityMapping.JiraIssuePriorityId = jiraIssuePriorityId;
                            if (jiraIssuePriorityId == null)
                                _v1Log.DebugFormat("Jira priority '{0}' not found. No priority will be set", priorityMapping.JiraPriority);
                        }
                    }
                }
            }
        }

        public void ValidateStatusMappings()
        {
            foreach (var serverSettings in JiraSettings.GetInstance().Servers.Cast<JiraServer>().Where(s => s.Enabled))
            {
                var jira = _jiraInstances.FirstOrDefault(j => j.InstanceUrl.Equals(serverSettings.Url));
                if (jira != null)
                {
                    foreach (var projectMapping in serverSettings.ProjectMappings.Cast<ProjectMapping>())
                    {
                        foreach (var statusMapping in projectMapping.StatusMappings.Cast<StatusMapping>()
                            .Where(sm => sm.Enabled))
                        {
                            var jiraStatusId = jira.GetStatusId(statusMapping.JiraStatus);
                            if (jiraStatusId == null)
                                _v1Log.DebugFormat("Jira status '{0}' not found. No status will be set", statusMapping.JiraStatus);

                            var v1StatusId = _v1.GetStatusIdFromName(statusMapping.V1Status).Result;
                            if (v1StatusId == null)
                                _v1Log.DebugFormat("Version One status '{0}' not found. No status will be set", statusMapping.V1Status);
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
                        _v1Log.Error(messageNode.Value);
                    }
                }
            }
        }
    }
}