using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Serializers;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraConnector.Exceptions;
using VersionOne.TeamSync.JiraConnector.Interfaces;

namespace VersionOne.TeamSync.JiraConnector.Connector
{
    public class JiraConnector : IJiraConnector
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(JiraConnector));
        private const string InQuery = "{0} in ({1})";

        private readonly IRestClient _client;
        private readonly ISerializer _serializer = new JiraSerializer();
        private readonly string _username;

        public string BaseUrl { get; private set; }

        public JiraConnector(JiraServer settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

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

            _client = new RestClient(new Uri(new Uri(settings.Url), "/rest/api/latest").ToString()) { Proxy = proxy };
            BaseUrl = settings.Url;

            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            {
                _client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);
                _username = settings.Username;
            }

            if (settings.IgnoreCertificate)
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, errors) => true;
        }

        public JiraConnector(string baseUrl, string username, string password)
            : this(new JiraServer
            {
                Url = baseUrl,
                Username = username,
                Password = password
            })
        {
        }

        public JiraConnector(IRestClient restClient)
        {
            _client = restClient;
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

        public string Get(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>))
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty; // TODO: Exception?

            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = path,
                RequestFormat = DataFormat.Json,
                JsonSerializer = _serializer
            };

            if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
                request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

            return Execute(request, HttpStatusCode.OK);
        }

        public T Get<T>(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>)) where T : new()
        {
            if (string.IsNullOrEmpty(path))
                return default(T); // TODO: Exception?

            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = path,
                RequestFormat = DataFormat.Json,
                JsonSerializer = _serializer
            };

            if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
                request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

            return Execute<T>(request, HttpStatusCode.OK);
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

        public SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties) //not entirely convinced this belongs here
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "search",
            };

            var queryString = string.Join(" AND ", query.Select(item => item.ToString()));
            request.AddQueryParameter("jql", queryString);
            request.AddQueryParameter("fields", string.Join(",", properties));

            var content = Execute(request, HttpStatusCode.OK);

            return JsonConvert.DeserializeObject<SearchResult>(content);
        }

        public SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties, Action<string, Fields, Dictionary<string, object>> customProperties) //not entirely convinced this belongs here
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "search?expand=renderedFields",
            };

            var queryString = string.Join(" AND ", query.Select(item => item.ToString()));
            request.AddQueryParameter("jql", queryString);
            request.AddQueryParameter("fields", string.Join(",", properties));

            var content = Execute(request, HttpStatusCode.OK);
            var result = JObject.Parse(content);
            var issues = result.Property("issues").Value;
            var searchResult = JsonConvert.DeserializeObject<SearchResult>(result.ToString());
            foreach (var issue in issues)
            {
                var fields = issue["fields"].ToObject<Dictionary<string, object>>();
                var key = issue["key"].ToString();
                customProperties(key, searchResult.issues.Single(i => i.Key == key).Fields, fields);
            }

            return searchResult;
        }
        
        public JiraVersionInfo GetVersionInfo()
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "serverInfo"
            };

            var content = Execute(request, HttpStatusCode.OK);

            return JsonConvert.DeserializeObject<JiraVersionInfo>(content);
        }

        public SearchResult GetSearchResults(IDictionary<string, IEnumerable<string>> query, IEnumerable<string> properties)
        {
            var content = Execute(BuildSearchRequest(query, properties), HttpStatusCode.OK);

            return JsonConvert.DeserializeObject<SearchResult>(content);
        }

        public CreateMeta GetCreateMetaInfoForProjects(IEnumerable<string> projectKey)
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "issue/createmeta"
            };

            request.AddQueryParameter("projectKeys", string.Join(",", projectKey));
            request.AddQueryParameter("expand", "projects.issuetypes.fields");

            var content = Execute(request, HttpStatusCode.OK);

            return JsonConvert.DeserializeObject<CreateMeta>(content);
        }

        public bool IsConnectionValid()
        {
            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = "user",
                RequestFormat = DataFormat.Json
            };
            request.AddQueryParameter("username", _username);

            var response = _client.Execute(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new JiraLoginException("Could not connect to Jira. Bad credentials.");

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                throw new JiraException("Could not connect to Jira. Bad url.");

            return response.StatusCode.Equals(HttpStatusCode.OK);
        }

        public bool ProjectExists(string projectIdOrKey)
        {
            var request = new RestRequest
            {
                Method = Method.GET,
                Resource = "project/{projectIdOrKey}",
                RequestFormat = DataFormat.Json
            };
            request.AddUrlSegment("projectIdOrKey", projectIdOrKey);

            try
            {
                Execute(request, HttpStatusCode.OK);
                return true;
            }
            catch (JiraException je)
            {
                if (je.StatusCode.Equals(HttpStatusCode.NotFound))
                    return false;

                throw;
            }
        }

        #region HELPER METHODS

        public static RestRequest BuildSearchRequest(IDictionary<string, IEnumerable<string>> query, IEnumerable<string> properties)// ..|..
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "search",
            };

            var queryString = string.Join(" AND ", query.Select(item =>
            {
                if (item.Value.Count() == 1)
                    return item.Key + "=" + item.Value.First().QuoteReservedWord();
                return string.Format(InQuery, item.Key, string.Join(", ", item.Value));
            }));

            request.AddQueryParameter("jql", queryString);
            request.AddQueryParameter("fields", string.Join(",", properties));
            return request;
        }

        private static Exception ProcessResponseError(IRestResponse response)
        {
            if (response.StatusCode.Equals(HttpStatusCode.BadRequest))
            {
                var error = JsonConvert.DeserializeObject<BadResult>(response.Content);
                if (error.Errors.Values.Any(x => x.Contains("It is not on the appropriate screen, or unknown.")))
                    return
                        new JiraException(
                            string.Format("Please expose the field {0} on the screen", error.Errors.First().Key),
                            new Exception(error.Errors.First().Value));
            }

            if (response.StatusCode.Equals(HttpStatusCode.Unauthorized))
                return new JiraLoginException();

            if (
                response.Headers.Any(
                    h => h.Name.Equals("X-Seraph-LoginReason") && h.Value.Equals("AUTHENTICATION_DENIED")))
                return
                    new JiraLoginException(
                        "Authentication to JIRA was denied. This may be a result of the CAPTCHA feature being triggered.");

            return new JiraException(response.StatusCode, response.StatusDescription, new Exception(response.Content));
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

            Log.Trace(stringBuilder.ToString());
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

            Log.Trace(stringBuilder.ToString());
        }

        #endregion
    }
}
