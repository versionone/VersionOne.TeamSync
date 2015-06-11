using System;
using System.Collections.Generic;
using System.Linq;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.Worker.Domain
{
    public class V1JiraInfo
    {
        private Jira jira;

        protected bool Equals(V1JiraInfo other)
        {
            return string.Equals(V1ProjectId, other.V1ProjectId) && 
                string.Equals(JiraKey, other.JiraKey) && 
                JiraInstance.InstanceUrl.Equals(other.JiraInstance.InstanceUrl);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = V1ProjectId.GetHashCode();
                hashCode = (hashCode*397) ^ JiraKey.GetHashCode();
                hashCode = (hashCode*397) ^ JiraInstance.InstanceUrl.GetHashCode();
                return hashCode;
            }
        }

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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((V1JiraInfo) obj);
        }

        public string V1ProjectId { get; private set; }
        public string JiraKey { get; private set; }
        public string EpicCategory { get; set; }
        public IJira JiraInstance { get; private set; }

        public static HashSet<V1JiraInfo> BuildJiraInfo(JiraServerCollection servers)
        {
            var list = new HashSet<V1JiraInfo>();

            for (var i = 0; i < JiraSettings.Settings.Servers.Count; i++)
            {
                var server = JiraSettings.Settings.Servers[i];
                if (!server.Enabled)
                    continue;

                var connector = new JiraConnector.Connector.JiraConnector(new Uri(new Uri(server.Url), "/rest/api/latest").ToString(), server.Username, server.Password);

                var projectMappings = new List<IProjectMapping>();

                for (var p = 0; p < server.ProjectMappings.Count; p++)
                {
                    if (!server.ProjectMappings[p].Enabled)
                        continue;
                    projectMappings.Add(server.ProjectMappings[p]);
                }

                var createMeta = connector.GetCreateMetaInfoForProjects(projectMappings.Select(map => map.JiraProject));

                projectMappings.ForEach(map =>
                {
                    var projectMeta = createMeta.Projects.Single(project => project.Key == map.JiraProject);
                    list.Add(new V1JiraInfo(map, new Jira(connector, projectMeta)));
                });

            }
            return list;
        }
    }
}
