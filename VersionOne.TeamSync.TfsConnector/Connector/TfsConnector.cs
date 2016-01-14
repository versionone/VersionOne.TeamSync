using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.TfsConnector.Entities;
using VersionOne.TeamSync.TfsConnector.Exceptions;
using VersionOne.TeamSync.TfsConnector.Interfaces;

namespace VersionOne.TeamSync.TfsConnector.Connector
{
    public class TfsConnector : ITfsConnector
    {
        public const string TfsRestApiUrl = "/tfs/DefaultCollection/_apis";
        public const string TfsApiVersion = "api-version=1.0";
        public const string TfsRestPath = "/tfs/DefaultCollection/_apis/wit";
        
        private readonly IRestClient _client;
        private readonly ISerializer _serializer = new TfsSerializer();

        private readonly IV1Log _v1Log;

        public string BaseUrl { get; set; }

        public string Username { get; set; }

        public string Password  { get; set; }
      
        [ImportingConstructor]
        public TfsConnector(IRestClient restClient, [Import] IV1LogFactory v1LogFactory)
        {
            if (v1LogFactory != null)
            _v1Log = v1LogFactory.Create<TfsConnector>();
            _client = restClient;
        }

        public TfsConnector(TfsServer settings, IV1LogFactory v1LogFactory = null)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            if (v1LogFactory != null)
                _v1Log = v1LogFactory.Create<TfsConnector>();

            WebProxy proxy = null;
            if (settings.Proxy != null && settings.Proxy.Enabled)
            {
                NetworkCredential cred;
                if (string.IsNullOrEmpty(settings.Proxy.Username))
                {
                    cred = (NetworkCredential)CredentialCache.DefaultCredentials;
                }
                else
                {
                    cred = new NetworkCredential(settings.Proxy.Username, settings.Proxy.Password);
                    if (!string.IsNullOrWhiteSpace(settings.Proxy.Domain))
                    {
                        cred.Domain = settings.Proxy.Domain;
                    }
                }

                proxy = new WebProxy(new Uri(settings.Proxy.Url), false, new string[] { }, cred);
            }

            _client = new RestClient(new Uri(settings.Url).ToString()) { Proxy = proxy };
            BaseUrl = settings.Url;

            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            {
                _client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);
                Username = settings.Username;
            }

            if (settings.IgnoreCertificate)
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, errors) => true;
        }

        public TfsConnector(string baseUrl, string username, string password)
            : this(new TfsServer { Url = baseUrl, Username = username, Password = password })
        {
        }

        #region EXECUTE

        public string Execute(IRestRequest request, HttpStatusCode responseStatusCode)
        {
            LogRequest(_client, request);

            var response = _client.Execute(request); // TODO: ExecuteAsync?

            LogResponse(response);

            if (response.StatusCode.Equals(responseStatusCode))
                return response.Content;

            throw ProcessResponseError(response);
        }

        public T Execute<T>(IRestRequest request, HttpStatusCode responseStatusCode) where T : new()
        {
            LogRequest(_client, request);

            var response = _client.Execute<T>(request); // TODO: ExecuteAsync?

            LogResponse(response);

            if (response.StatusCode.Equals(responseStatusCode))
                return response.Data;

            throw ProcessResponseError(response);
        }

        #endregion

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

        
        public bool IsConnectionValid()
        {
            var path = string.Format("{0}/projects?{1}&$top=1", TfsRestApiUrl, TfsApiVersion);

            var request = BuildGetRequest(path, default(KeyValuePair<string, string>), default(IDictionary<string, string>));
            LogRequest(_client, request);

            var response = _client.Execute(request);
            LogResponse(response);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new TfsLoginException("Could not connect to TFS. Bad credentials.");

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                throw new TfsException("Could not connect to TFS. Bad url.");

            return response.StatusCode.Equals(HttpStatusCode.OK);
        }

        public bool ProjectExists(string projectIdOrKey)
        {
            var path = string.Format("{0}/projects/{1}?{2}", TfsRestApiUrl, projectIdOrKey, TfsApiVersion);
            var request = BuildGetRequest(path, default(KeyValuePair<string, string>), default(IDictionary<string, string>));

            try
            {
                Execute(request, HttpStatusCode.OK);
                return true;
            }
            catch (TfsException e)
            {
                if (e.StatusCode.Equals(HttpStatusCode.NotFound))
                    return false;

                throw new TfsException("TFS project not found."); ;
            }
        }

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

        private static Exception ProcessResponseError(IRestResponse response)
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
                    h => h.Name.Equals("X-Seraph-LoginReason") && h.Value.Equals("AUTHENTICATION_DENIED")))
                return
                    new TfsLoginException(
                        "Authentication to Tfs was denied. This may be a result of the CAPTCHA feature being triggered.");

            return new TfsException(response.StatusCode, response.StatusDescription, new Exception(response.Content));
        }

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

        private void LogRequest(IRestClient client, IRestRequest req)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("REQUEST");
            stringBuilder.AppendLine("\tMethod: " + req.Method);
            stringBuilder.AppendLine("\tRequest URL: " + client.BaseUrl + "/" + req.Resource);
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
