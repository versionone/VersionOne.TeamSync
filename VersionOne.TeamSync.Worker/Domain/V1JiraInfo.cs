using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using VersionOne.TeamSync.JiraConnector.Config;

namespace VersionOne.TeamSync.Worker.Domain
{
    public class V1JiraInfo
    {
        private static ILog _log = LogManager.GetLogger(typeof(V1JiraInfo));

        public string V1ProjectId { get; private set; }
        public string JiraKey { get; private set; }
        public string EpicCategory { get; set; }
        public IJira JiraInstance { get; private set; }

        public V1JiraInfo(string v1ProjectId, string jiraKey, string epicCategory, IJira jiraInstance)
        {
            V1ProjectId = v1ProjectId;
            JiraKey = jiraKey;
            EpicCategory = epicCategory;
            JiraInstance = jiraInstance;
        }

        public V1JiraInfo(IProjectMapping projectMapping, IJira jiraInstance)
        {
            V1ProjectId = projectMapping.V1Project;
            JiraKey = projectMapping.JiraProject;
            EpicCategory = projectMapping.EpicSyncType;
            JiraInstance = jiraInstance;
        }

        public static IEnumerable<V1JiraInfo> BuildJiraInfo(JiraServerCollection servers, string minuteInterval)
        {
            var list = new HashSet<V1JiraInfo>();

            foreach (var server in servers.Cast<JiraServer>().Where(s => s.Enabled))
            {
                var connector =
                    new JiraConnector.Connector.JiraConnector(
                        new Uri(new Uri(server.Url), "/rest/api/latest").ToString(), server.Username, server.Password);

                var projectMappings = server.ProjectMappings.Cast<ProjectMapping>().Where(p => p.Enabled).ToList();
                if (projectMappings.Any())
                    projectMappings.ForEach(pm => list.Add(new V1JiraInfo(pm, new Jira(connector, pm.JiraProject))));
                else
                    _log.ErrorFormat("Jira server '{0}' requires that project mappings are set in the configuration file.", server.name);
            }

            return list;
        }

        public void ValidateConnection()
        {
            JiraInstance.ValidateConnection();
        }

        public bool ValidateMapping(IV1 v1)
        {
            return JiraInstance.ValidateProjectExists() && v1.ValidateProjectExists(V1ProjectId) && v1.ValidateEpicCategoryExists(EpicCategory);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = V1ProjectId.GetHashCode();
                hashCode = (hashCode * 397) ^ JiraKey.GetHashCode();
                hashCode = (hashCode * 397) ^ JiraInstance.InstanceUrl.GetHashCode();
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((V1JiraInfo)obj);
        }

        protected bool Equals(V1JiraInfo other)
        {
            return string.Equals(V1ProjectId, other.V1ProjectId) &&
                string.Equals(JiraKey, other.JiraKey) &&
                JiraInstance.InstanceUrl.Equals(other.JiraInstance.InstanceUrl);
        }
    }
}
