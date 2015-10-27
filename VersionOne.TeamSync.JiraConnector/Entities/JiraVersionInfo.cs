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

    public static class JiraVersionItems
    {
        public static Dictionary<string, string[]> VersionDoneWords = new Dictionary<string, string[]>
        {
            {"5", new[] {"Closed", "Close Issue"}},
            {"6", new[] {"Done", "Closed", "Close Issue"}}
        };  
    }
}
