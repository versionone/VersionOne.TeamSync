using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Serializers;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraConnector.Exceptions;

namespace VersionOne.TeamSync.JiraConnector.Connector
{
    public interface IJiraConnector
    {
        string BaseUrl { get; }
        void Execute(IRestRequest request, HttpStatusCode responseStatusCode);
        T ExecuteWithReturn<T>(IRestRequest request, HttpStatusCode responseStatusCode, Func<string, T> returnBuilder);
        ItemBase Post<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        void Put<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        void Delete(string path, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties);
        SearchResult GetSearchResults(IDictionary<string, IEnumerable<string>> query, IEnumerable<string> properties);
        CreateMeta GetCreateMetaInfoForProjects(IEnumerable<string> projectKey);

        SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties, Action<Fields, Dictionary<string, object>> customProperties) //not entirely convinced this belongs here
            ;
    }

    public class JiraConnector : IJiraConnector
    {
        private const string InQuery = "{0} in ({1})";

        private readonly IRestClient _client;
        private readonly ISerializer _serializer = new JiraSerializer();

        public JiraConnector(string baseUrl)
            : this(baseUrl, string.Empty, string.Empty)
        {
            _client = new RestClient(baseUrl);
            BaseUrl = _client.BaseUrl.AbsoluteUri.Replace(_client.BaseUrl.AbsolutePath, "");
        }

        public JiraConnector(string baseUrl, string username, string password)
        {
            _client = new RestClient(baseUrl);
            BaseUrl = _client.BaseUrl.AbsoluteUri.Replace(_client.BaseUrl.AbsolutePath, "");
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                _client.Authenticator = new HttpBasicAuthenticator(username, password);
            }
        }

        public JiraConnector(IRestClient restClient)
        {
            _client = restClient;
        }

        public string BaseUrl { get; private set; }

        public void Execute(IRestRequest request, HttpStatusCode responseStatusCode)
        {
            var response = _client.Execute(request); // TODO: ExecuteAsync?

            if (response.StatusCode.Equals(responseStatusCode))
                return;

            throw ProcessResponseError(response);
        }

        public T ExecuteWithReturn<T>(IRestRequest request, HttpStatusCode responseStatusCode, Func<string, T> returnBuilder)
        {
            var response = _client.Execute(request); // TODO: ExecuteAsync?

            if (response.StatusCode.Equals(responseStatusCode))
                return returnBuilder(response.Content);

            throw ProcessResponseError(response);
        }

        public ItemBase Post<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>))
        {
            if (string.IsNullOrEmpty(path))
                return null; // TODO: Exception?

            if (data == null)
                return null; // TODO: Exception?

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

            return ExecuteWithReturn(request, responseStatusCode, JsonConvert.DeserializeObject<ItemBase>);
        }

        public void Put<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>))
        {
            if (string.IsNullOrEmpty(path))
                return; // TODO: Exception?

            if (data == null)
                return; // TODO: Exception?

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

            Execute(request, responseStatusCode);
        }

        public void Delete(string path, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>))
        {
            if (string.IsNullOrEmpty(path))
                return; // TODO: Exception?

            var request = new RestRequest
            {
                Method = Method.DELETE,
                Resource = path
            };

            if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
                request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

            Execute(request, responseStatusCode);
        }

        public SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties) //not entirely convinced this belongs here
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "search",
            };

            var queryString = string.Join(" AND ", query.Select(item => item.ToString()));
            request.AddQueryParameter("jql", queryString);
            request.AddQueryParameter("fields", string.Join(",", properties));

            return ExecuteWithReturn(request, HttpStatusCode.OK, JsonConvert.DeserializeObject<SearchResult>);
        }

        public SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties, Action<Fields, Dictionary<string, object>> customProperties) //not entirely convinced this belongs here
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "search",
            };

            var queryString = string.Join(" AND ", query.Select(item => item.ToString()));
            request.AddQueryParameter("jql", queryString);
            request.AddQueryParameter("fields", string.Join(",", properties));

            var result = ExecuteWithReturn(request, HttpStatusCode.OK, JObject.Parse);
            var issues = result.Property("issues").Value;
            var searchResult = JsonConvert.DeserializeObject<SearchResult>(result.ToString());
            foreach (var issue in issues)
            {
                var fields = issue["fields"].ToObject<Dictionary<string, object>>();
                customProperties(searchResult.issues.Single(i => i.Key == issue["key"].ToString()).Fields, fields);
            }

            return searchResult;
        }

        public SearchResult GetSearchResults(IDictionary<string, IEnumerable<string>> query, IEnumerable<string> properties)
        {
            var request = BuildSearchRequest(query, properties);

            return ExecuteWithReturn(request, HttpStatusCode.OK, JsonConvert.DeserializeObject<SearchResult>);
        }

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
                    return new JiraException(string.Format("Please expose the field {0} on the screen", error.Errors.First().Key), new Exception(error.Errors.First().Value));
            }

            if (response.StatusCode.Equals(HttpStatusCode.Unauthorized))
                return new JiraLoginException();

            if (response.Headers.Any(h => h.Name.Equals("X-Seraph-LoginReason") || h.Value.Equals("AUTHENTICATION_DENIED")))
                return new JiraLoginException("Authentication to JIRA was denied. This may be be a result of the CAPTCHA feature being triggered.");

            return new JiraException(response.StatusDescription, new Exception(response.Content));
        }
		
		public CreateMeta GetCreateMetaInfoForProjects(IEnumerable<string> projectKey)
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "issue/createmeta"
            };

            request.AddQueryParameter("projectKeys", string.Join(",", projectKey));
            request.AddQueryParameter("expand", "projects.issuetypes.fields");

            return ExecuteWithReturn(request, HttpStatusCode.OK, JsonConvert.DeserializeObject<CreateMeta>);
        }
    }
}
