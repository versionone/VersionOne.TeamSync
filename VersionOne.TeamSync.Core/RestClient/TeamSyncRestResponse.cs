using RestSharp;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using VersionOne.TeamSync.Interfaces.RestClient;

namespace VersionOne.TeamSync.Core.RestClient
{
	public class TeamSyncRestResponse : ITeamSyncRestResponse
	{
		public string Content { get; protected set; }
		public HttpStatusCode StatusCode { get; protected set; }
		public IEnumerable<KeyValuePair<string, string>> Headers { get; protected set; }
		public string ErrorMessage { get; set; }
		public string StatusDescription { get; protected set; }

		public TeamSyncRestResponse(IRestResponse response)
		{
			Content = response.Content;
			StatusCode = response.StatusCode;
			Headers =
				from p in response.Headers
				where p.Type == ParameterType.HttpHeader
				select new KeyValuePair<string, string>(p.Name, (string)p.Value);
			ErrorMessage = response.ErrorMessage;
			StatusDescription = response.StatusDescription;
		}
	}
}