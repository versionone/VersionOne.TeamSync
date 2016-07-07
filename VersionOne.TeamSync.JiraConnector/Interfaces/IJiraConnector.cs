using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;
using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.JiraConnector.Interfaces
{
    public interface IJiraConnector
    {
        string BaseUrl { get; }
        string Username { get; }

        T Execute<T>(IRestRequest request, HttpStatusCode responseStatusCode) where T : new();
        string Execute(IRestRequest request, HttpStatusCode responseStatusCode);

        string Get(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>), Dictionary<string, string> queryParameters = null);
        T Get<T>(string path, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>), Dictionary<string, string> queryParameters = null) where T : new();
        string Post(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        T Post<T>(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>)) where T : new();
        string Put(string path, object data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        string Delete(string path, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));

	    SearchResult GetAllSearchResults(IList<JqOperator> query, IEnumerable<string> properties);
        SearchResult GetSearchResults(IDictionary<string, IEnumerable<string>> query, IEnumerable<string> properties);
        SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties);
        SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties, Action<string, Fields, Dictionary<string, object>> customProperties);
        CreateMeta GetCreateMetaInfoForProjects(IEnumerable<string> projectKey);

        bool IsConnectionValid();
        bool ProjectExists(string projectIdOrKey);
        JiraVersionInfo GetVersionInfo();
        UserInfo GetUserInfo();
    }
}