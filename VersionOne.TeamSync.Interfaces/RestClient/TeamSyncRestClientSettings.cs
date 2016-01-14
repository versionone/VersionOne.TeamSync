using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.TeamSync.Interfaces.RestClient
{
	public class TeamSyncRestClientSettings
	{	
		public string ApiBasicAuthUsername { get; set; }
		public string ApiBasicAuthPassword { get; set; }
		public string ApiBaseUrl { get; set; }
		public string ProxyuUsername { get; set; }
		public string ProxyPassword { get; set; }
		public bool ProxyEnabled { get; set; }
		public string ProxyDomain { get; set; }
		public string ProxyUrl { get; set; }
		public Func<ITeamSyncRestResponse, Exception> ErrorHandler { get; set; }
		public IV1Log Log { get; set; }
		public bool IgnoreCertificate { get; set; }
	}
}