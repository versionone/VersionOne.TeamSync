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

        private readonly IV1Log _v1Log;

        [ImportingConstructor]
        public TfsConnector(IRestClient restClient, [Import] IV1LogFactory v1LogFactory)
        {
            _v1Log = v1LogFactory.Create<TfsConnector>();
            _client = restClient;
        }

        //public const string JiraRestApiUrl = "api/latest";
        //public const string JiraAgileApiUrl = "agile/latest";
        //public const string InQuery = "{0} in ({1})";

        //private static ILog Log = LogManager.GetLogger(typeof(JiraConnector));

        private readonly IRestClient _client;
        private readonly ISerializer _serializer = new TfsSerializer();
       //public TfsConnector(TfsServer settings)
        //{
        //    if (settings == null)
        //        throw new ArgumentNullException("settings");

        //    WebProxy proxy = null;
        //    if (settings.Proxy != null && settings.Proxy.Enabled)
        //    {
        //        NetworkCredential cred;
        //        if (string.IsNullOrEmpty(settings.Proxy.Username))
        //        {
        //            cred = (NetworkCredential)CredentialCache.DefaultCredentials;
        //        }
        //        else
        //        {
        //            cred = new NetworkCredential(settings.Proxy.Username, settings.Proxy.Password);
        //            if (!string.IsNullOrWhiteSpace(settings.Proxy.Domain))
        //            {
        //                cred.Domain = settings.Proxy.Domain;
        //            }
        //        }

        //        proxy = new WebProxy(new Uri(settings.Proxy.Url), false, new string[] { }, cred);
        //    }

        //    _client = new RestClient(new Uri(new Uri(settings.Url), "/rest").ToString()) { Proxy = proxy };
        //    BaseUrl = settings.Url;

        //    if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
        //    {
        //        _client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);
        //        Username = settings.Username;
        //    }

        //    if (settings.IgnoreCertificate)
        //        ServicePointManager.ServerCertificateValidationCallback =
        //            (sender, certificate, chain, errors) => true;
        //}

     

        //public TfsConnector(IRestClient restClient, ILog logger)
        //{
        //    _client = restClient;
        //    Log = logger;
        //}


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

    

        public string BaseUrl
        {
            get { throw new NotImplementedException(); }
        }

        public string Username
        {
            get { throw new NotImplementedException(); }
        }

        public string Password
        {
            get { throw new NotImplementedException(); }
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

        public bool IsConnectionValid()
        {
            throw new NotImplementedException();
        }

        public bool ProjectExists(string projectIdOrKey)
        {
            throw new NotImplementedException();
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
