using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.JiraConnector.Config;

namespace VersionOne.Integration.Service.Worker.Domain
{
    public class V1JiraInfo
    {
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

                for (var p = 0; p < server.ProjectMappings.Count; p++)
                {
                    if (server.ProjectMappings[p].Enabled)
                        list.Add(new V1JiraInfo(
                            server.ProjectMappings[p].V1Project,
                            server.ProjectMappings[p].JiraProject,
                            server.ProjectMappings[p].EpicSyncType,
                            new Jira(new JiraConnector.Connector.JiraConnector(new Uri(new Uri(server.Url), "/rest/api/latest").ToString(), server.Username, server.Password))));
                }
            }
            return list;
        }
    }
}
