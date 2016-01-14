using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.TeamSync.Interfaces.RestClient
{
	public interface ITeamSyncRestClient
	{
		string Delete(string path, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
		string Get(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>), IDictionary<string, string> queryParameters = null);
		T Get<T>(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>), IDictionary<string, string> queryParameters = null) where T : new();
		string Post(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
		T Post<T>(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>)) where T : new();
		string Put(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
		ITeamSyncRestResponse Execute(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>), IDictionary<string, string> queryParameters = default(IDictionary<string, string>));
	}

}
