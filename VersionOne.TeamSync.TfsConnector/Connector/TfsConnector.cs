using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.TfsConnector.Entities;
using VersionOne.TeamSync.TfsConnector.Exceptions;
using VersionOne.TeamSync.TfsConnector.Interfaces;
using VersionOne.TeamSync.Interfaces.RestClient;

namespace VersionOne.TeamSync.TfsConnector.Connector
{
    public class TfsConnector : ITfsConnector
    {
		public const string TfsRestApiUrl = "/tfs/DefaultCollection/_apis";
		public const string TfsApiVersion = "api-version=1.0";
		public const string TfsRestPath = "/tfs/DefaultCollection/_apis/wit";

        private readonly IV1Log _v1Log;
		private readonly ITeamSyncRestClient _client;
		public TfsConnector(TfsServer settings, IV1LogFactory v1LogFactory, ITeamSyncRestClientFactory restClientFactory)
		{
			_v1Log = v1LogFactory.Create<TfsConnector>();

			if (settings == null)
				throw new ArgumentNullException("settings");

			BaseUrl = settings.Url;

			var restClientSettings = new TeamSyncRestClientSettings
			{
				Log = _v1Log,
				ApiBaseUrl = BaseUrl,
				ApiBasicAuthUsername = settings.Username,
				ApiBasicAuthPassword = settings.Password,
				ProxyEnabled = settings.Proxy.Enabled,
				ProxyDomain = settings.Proxy.Domain,
				ProxyUrl = settings.Proxy.Url,
				ProxyuUsername = settings.Proxy.Username,
				ProxyPassword = settings.Proxy.Password,
				ErrorHandler = ProcessResponseError
			};

			_client = restClientFactory.Create(restClientSettings);
		}

		public string BaseUrl { get; private set; }

		private static Exception ProcessResponseError(ITeamSyncRestResponse response)
		{
			if (response.StatusCode.Equals(HttpStatusCode.BadRequest))
			{
				var error = JsonConvert.DeserializeObject<BadResult>(response.Content);
				if (error.Errors.Values.Any(x => x.Contains("It is not on the appropriate screen, or unknown.")))
					return
						new TfsException(
							string.Format("Please expose the field {0} on the screen", error.Errors.First().Key),
							new Exception(error.Errors.First().Value));
			}

			if (response.StatusCode.Equals(HttpStatusCode.Unauthorized))
				return new TfsLoginException();

			if (
				response.Headers.Any(
					h => h.Key.Equals("X-Seraph-LoginReason") && h.Value.Equals("AUTHENTICATION_DENIED")))
				return
					new TfsLoginException(
						"Authentication to Tfs was denied. This may be a result of the CAPTCHA feature being triggered.");

			return new TfsException(response.StatusCode, response.StatusDescription, new Exception(response.Content));
		}

		//public TfsConnector(string baseUrl, string username, string password)
		//	: this(new TfsServer { Url = baseUrl, Username = username, Password = password })
		//{
		//}
		public bool IsConnectionValid()
		{
			var path = string.Format("{0}/projects?{1}&$top=1", TfsRestApiUrl, TfsApiVersion);

			var response = _client.Execute(path);

			if (response.StatusCode == HttpStatusCode.Unauthorized)
				throw new TfsLoginException("Could not connect to TFS. Bad credentials.");

			if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
				throw new TfsException("Could not connect to TFS.", new Exception(response.ErrorMessage));

			return response.StatusCode.Equals(HttpStatusCode.OK);
		}

		public bool ProjectExists(string projectIdOrKey)
		{
			var path = string.Format("{0}/projects/{1}?{2}", TfsRestApiUrl, projectIdOrKey, TfsApiVersion);
            
			try
			{
				var response = _client.Execute(path);
				return response.StatusCode.Equals(HttpStatusCode.OK);
			}
			catch (TfsException e)
			{
				if (e.StatusCode.Equals(HttpStatusCode.NotFound))
					return false;

			    throw;
			}
		}
	}
}