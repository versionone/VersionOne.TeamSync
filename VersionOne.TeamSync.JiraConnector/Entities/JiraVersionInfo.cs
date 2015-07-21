using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.TeamSync.JiraConnector.Entities
{
    public class JiraVersionInfo
    {
        public string Version { get; set; }
        public string BuildNumber { get; set; }
        public string[] VersionNumbers { get; set; }
    }
}
