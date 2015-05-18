using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;
using VersionOne.SDK.Jira.Exceptions;

namespace VersionOne.SDK.Jira.Connector
{
    public class JiraConnector
    {
        private readonly RestClient _client;

        public JiraConnector(string baseUrl)
            : this(baseUrl, string.Empty, string.Empty)
        {
        }

        public JiraConnector(string baseUrl, string username, string password)
        {
            _client = new RestClient(baseUrl);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                _client.Authenticator = new HttpBasicAuthenticator(username, password);
            }
        }

        private void Execute(RestRequest request, HttpStatusCode responseStatusCode)
        {
            var response = _client.Execute(request); // TODO: ExecuteAsync?

            if (response.StatusCode.Equals(responseStatusCode))
                return;
            if (response.StatusCode.Equals(HttpStatusCode.Unauthorized))
                throw new JiraLoginException();
            throw new JiraException(response.StatusDescription, new Exception(response.Content));
        }

        public void Post<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>))
        {
            if (string.IsNullOrEmpty(path))
                return; // TODO: Exception?

            if (data == null)
                return; // TODO: Exception?

            var request = new RestRequest
            {
                Method = Method.POST,
                Resource = path,
                RequestFormat = DataFormat.Json,
            };

            if (!urlSegment.Equals(default(KeyValuePair<string, string>)))
                request.AddUrlSegment(urlSegment.Key, urlSegment.Value);

            request.AddBody(data);

            Execute(request, responseStatusCode);
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
    }
}
