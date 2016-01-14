using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.TeamSync.Interfaces.RestClient
{
	public interface ITeamSyncRestResponse
	{
		string Content { get; }
		HttpStatusCode StatusCode { get; }
		IEnumerable<KeyValuePair<string, string>> Headers { get; }
		string ErrorMessage { get; }
		string StatusDescription { get; }
	}
}
