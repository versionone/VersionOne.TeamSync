using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers;
using VersionOne.SDK.Jira.Entities;
using VersionOne.SDK.Jira.Exceptions;

namespace VersionOne.SDK.Jira.Connector
{
    public interface IJiraConnector
    {
        string BaseUrl { get; }
        void Execute(RestRequest request, HttpStatusCode responseStatusCode);
        T ExecuteWithReturn<T>(RestRequest request, HttpStatusCode responseStatusCode, Func<string, T> returnBuilder);
        ItemBase Post<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        void Put<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        void Delete(string path, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        SearchResult GetSearchResults(IDictionary<string, string> query, IEnumerable<string> properties);
        SearchResult GetSearchResults(IDictionary<string, IEnumerable<string>> query, IEnumerable<string> properties);
    }

    public class JiraConnector : IJiraConnector
    {
        private readonly RestClient _client;
        private ISerializer _serializer = new JiraSerializer();

        public JiraConnector(string baseUrl) : this(baseUrl, string.Empty, string.Empty)
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

        public string BaseUrl { get; private set; }

        public void Execute(RestRequest request, HttpStatusCode responseStatusCode)
        {
            var response = _client.Execute(request); // TODO: ExecuteAsync?

            if (response.StatusCode.Equals(responseStatusCode))
                return;
            if (response.StatusCode.Equals(HttpStatusCode.Unauthorized))
                throw new JiraLoginException();

            var error = JsonConvert.DeserializeObject<BadResult>(response.Content);
            if (error.Errors.Values.Any(x => x.Contains("It is not on the appropriate screen, or unknown.")))
                throw new JiraException("Please expose the field " + error.Errors.First().Key + " on the screen", new Exception(error.Errors.First().Value));

            throw new JiraException(response.StatusDescription, new Exception(response.Content));
        }

        public T ExecuteWithReturn<T>(RestRequest request, HttpStatusCode responseStatusCode, Func<string, T> returnBuilder)
        {
            var response = _client.Execute(request); // TODO: ExecuteAsync?
            if (response.StatusCode.Equals(responseStatusCode) || response.StatusCode.Equals(HttpStatusCode.BadRequest))
                return returnBuilder(response.Content);
            if (response.StatusCode.Equals(HttpStatusCode.Unauthorized))
                throw new JiraLoginException();
            throw new JiraException(response.StatusDescription, new Exception(response.Content));
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

            return ExecuteWithReturn(request, responseStatusCode, Newtonsoft.Json.JsonConvert.DeserializeObject<ItemBase>);
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

        public SearchResult GetSearchResults(IDictionary<string, string> query, IEnumerable<string> properties) //not entirely convinced this belongs here
        {
            var request = new RestRequest(Method.GET)
            {
                Resource = "search",
            };

            var queryString = string.Join(" AND ", query.Select(item => item.Key + "=" + item.Value));
            request.AddQueryParameter("jql", queryString);
            request.AddQueryParameter("fields", string.Join(",", properties));

            return ExecuteWithReturn(request, HttpStatusCode.OK, Newtonsoft.Json.JsonConvert.DeserializeObject<SearchResult>);
        }

        private const string _inQuery = "{0} in ({1})";

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
                return string.Format(_inQuery, item.Key, string.Join(", ", item.Value));
            }));

            request.AddQueryParameter("jql", queryString);
            request.AddQueryParameter("fields", string.Join(",", properties));
            return request;
        }
    }
}
