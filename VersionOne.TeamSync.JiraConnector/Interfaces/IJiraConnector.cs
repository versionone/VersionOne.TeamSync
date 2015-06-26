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
        void Execute(IRestRequest request, HttpStatusCode responseStatusCode);
        T ExecuteWithReturn<T>(IRestRequest request, HttpStatusCode responseStatusCode, Func<string, T> returnBuilder);
        ItemBase Post<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        void Put<T>(string path, T data, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        void Delete(string path, HttpStatusCode responseStatusCode, KeyValuePair<string, string> urlSegment = default(KeyValuePair<string, string>));
        SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties);
        SearchResult GetSearchResults(IDictionary<string, IEnumerable<string>> query, IEnumerable<string> properties);
        CreateMeta GetCreateMetaInfoForProjects(IEnumerable<string> projectKey);

        SearchResult GetSearchResults(IList<JqOperator> query, IEnumerable<string> properties,
            Action<Fields, Dictionary<string, object>> customProperties); //not entirely convinced this belongs here

        bool IsConnectionValid();
        bool ProjectExists(string projectIdOrKey);
    }
}