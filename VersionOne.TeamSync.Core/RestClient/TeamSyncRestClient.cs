using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.Interfaces.RestClient;

namespace VersionOne.TeamSync.Core.RestClient
{
	public class TeamSyncRestClient : ITeamSyncRestClient
	{
		private IV1Log _v1Log;
		private IRestClient _client;
		private ISerializer _serializer;
		private Func<ITeamSyncRestResponse, Exception> _errorProcessor;

		public TeamSyncRestClient(TeamSyncRestClientSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			if (settings.Log == null)
				throw new ArgumentNullException("settings.Log");
			else
				_v1Log = settings.Log;

			if (settings.ErrorHandler == null)
				throw new ArgumentNullException("settings.ErrorHandler");
			else
				_errorProcessor = settings.ErrorHandler;

			IWebProxy proxy = null;
			if (settings.ProxyEnabled)
			{
				NetworkCredential cred;
				if (string.IsNullOrEmpty(settings.ProxyuUsername))
				{
					cred = (NetworkCredential)CredentialCache.DefaultCredentials;
				}
				else
				{
					cred = new NetworkCredential(settings.ProxyuUsername, settings.ProxyPassword);
					if (!string.IsNullOrWhiteSpace(settings.ProxyDomain))
					{
						cred.Domain = settings.ProxyDomain;
					}
				}

				proxy = new WebProxy(new Uri(settings.ProxyUrl), false, new string[] { }, cred);
			}

			_client = new RestSharp.RestClient(new Uri(settings.ApiBaseUrl).ToString()) { Proxy = proxy };

			if (!string.IsNullOrEmpty(settings.ApiBasicAuthUsername) && !string.IsNullOrEmpty(settings.ApiBasicAuthPassword))
			{
				_client.Authenticator = new HttpBasicAuthenticator(settings.ApiBasicAuthUsername, settings.ApiBasicAuthPassword);
			}

			if (settings.IgnoreCertificate)
				ServicePointManager.ServerCertificateValidationCallback =
					(sender, certificate, chain, errors) => true;

			_serializer = new TeamSyncRestClientDefaultJsonSerializer();
		}

		#region HTTP VERBS

		public string Get(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>), IDictionary<string, string> queryParameters = null)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty; // TODO: Exception?

			return Execute(BuildGetRequest(path, urlSegment, queryParameters), HttpStatusCode.OK);
		}

		public T Get<T>(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>), IDictionary<string, string> queryParameters = null) where T : new()
		{
			if (string.IsNullOrEmpty(path))
				return default(T); // TODO: Exception?

			return Execute<T>(BuildGetRequest(path, urlSegment, queryParameters), HttpStatusCode.OK);
		}

		public string Post(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>))
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty; // TODO: Exception?

			if (data == null)
				return string.Empty; // TODO: Exception?

			var request = new RestRequest
			{
				Method = Method.POST,
				Resource = path,
				RequestFormat = DataFormat.Json,
				JsonSerializer = _serializer
			};

			if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
				request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

			request.AddBody(data);

			return Execute(request, responseStatusCode);
		}

		public T Post<T>(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>)) where T : new()
		{
			if (string.IsNullOrEmpty(path))
				return default(T); // TODO: Exception?

			if (data == null)
				return default(T); // TODO: Exception?

			var request = new RestRequest
			{
				Method = Method.POST,
				Resource = path,
				RequestFormat = DataFormat.Json,
				JsonSerializer = _serializer
			};

			if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
				request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

			request.AddBody(data);

			return Execute<T>(request, responseStatusCode);
		}

		public string Put(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>))
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty; // TODO: Exception?

			if (data == null)
				return string.Empty; // TODO: Exception?

			var request = new RestRequest
			{
				Method = Method.PUT,
				Resource = path,
				RequestFormat = DataFormat.Json,
				JsonSerializer = _serializer
			};

			if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
				request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

			request.AddBody(data);

			return Execute(request, responseStatusCode);
		}

		public string Delete(string path, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>))
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty; // TODO: Exception?

			var request = new RestRequest
			{
				Method = Method.DELETE,
				Resource = path
			};

			if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
				request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

			return Execute(request, responseStatusCode);
		}

		#endregion

		#region HELPER METHODS

		private RestRequest BuildGetRequest(string path, KeyValuePair<string, string> urlSegment, IDictionary<string, string> queryParameters)
		{
			var request = new RestRequest
			{
				Method = Method.GET,
				Resource = path,
				RequestFormat = DataFormat.Json,
				JsonSerializer = _serializer
			};

			if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
				request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

			if (queryParameters != null)
				foreach (var param in queryParameters)
					request.AddQueryParameter(param.Key, param.Value);

			return request;
		}

		protected Exception ProcessResponseError(ITeamSyncRestResponse response)
		{
			if (_errorProcessor != null) throw _errorProcessor(response);
			else throw new Exception("Encountered an unexpected exception"); // TODO clean this up
		}

		#region EXECUTE

		public ITeamSyncRestResponse Execute(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>), IDictionary<string, string> queryParameters = default(IDictionary<string, string>)) {
			
			var request = BuildGetRequest(path, urlSegment, queryParameters);

			LogRequest(request);

			var response = _client.Execute(request); // TODO: ExecuteAsync?

			LogResponse(response);

			return new TeamSyncRestResponse(response);
		}

		protected virtual string Execute(IRestRequest request, HttpStatusCode responseStatusCode)
		{
			LogRequest(request);

			var response = _client.Execute(request); // TODO: ExecuteAsync?

			LogResponse(response);

			if (response.StatusCode.Equals(responseStatusCode))
				return response.Content;

			var teamSyncRestResponse = new TeamSyncRestResponse(response);

			throw ProcessResponseError(teamSyncRestResponse);
		}

		protected virtual T Execute<T>(IRestRequest request, HttpStatusCode responseStatusCode) where T : new()
		{
			LogRequest(request);

			var response = _client.Execute<T>(request);

			LogResponse(response);

			if (response.StatusCode.Equals(responseStatusCode))
				return response.Data;
			
			var teamSyncRestResponse = new TeamSyncRestResponse(response);

			throw ProcessResponseError(teamSyncRestResponse);
		}

		#endregion

		private void LogResponse(IRestResponse resp)
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("RESPONSE");
			stringBuilder.AppendLine("\tStatus code: " + resp.StatusCode);
			stringBuilder.AppendLine("\tHeaders: ");
			foreach (var header in resp.Headers)
			{
				stringBuilder.AppendLine("\t\t" + header.Name + "=" + string.Join(", ", header.Value));
			}
			stringBuilder.AppendLine("\tBody: ");
			stringBuilder.AppendLine("\t\t" + resp.Content);

			_v1Log.Trace(stringBuilder.ToString());
		}

		private void LogRequest(IRestRequest req)
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("REQUEST");
			stringBuilder.AppendLine("\tMethod: " + req.Method);
			stringBuilder.AppendLine("\tRequest URL: " + _client.BaseUrl + "/" + req.Resource);
			stringBuilder.AppendLine("\tHeaders: ");
			foreach (var parameter in req.Parameters.Where(param => param.Type == ParameterType.HttpHeader))
			{
				stringBuilder.AppendLine("\t\t" + parameter.Name + "=" + string.Join(", ", parameter.Value));
			}
			stringBuilder.AppendLine("\tQuery params: ");
			foreach (var parameter in req.Parameters.Where(param => param.Type == ParameterType.QueryString))
			{
				stringBuilder.AppendLine("\t\t" + parameter.Name + "=" + string.Join(", ", parameter.Value));
			}

			stringBuilder.AppendLine("\tBody: ");
			var reqBody = req.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			stringBuilder.AppendLine("\t\t" + reqBody);

			_v1Log.Trace(stringBuilder.ToString());
		}

		#endregion
	}
}