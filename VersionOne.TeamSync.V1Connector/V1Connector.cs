using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using log4net;
using VersionOne.TeamSync.Core;
using VersionOne.TeamSync.V1Connector.Extensions;
using VersionOne.TeamSync.V1Connector.Interfaces;

namespace VersionOne.TeamSync.V1Connector
{
    public class V1Connector : IV1Connector
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(V1Connector));
        private readonly HttpClient _client;
        private readonly HttpClientHandler _handler;
        private readonly Uri _baseAddress;
        private static ICredentials _networkCreds;
        private string _endpoint = "rest-1.v1/Data";
        private string _upstreamUserAgent;

        public V1Connector(string instanceUrl)
        {
            if (string.IsNullOrWhiteSpace(instanceUrl))
                throw new ArgumentNullException("instanceUrl");
            if (!instanceUrl.EndsWith("/"))
                instanceUrl += "/";

            if (Uri.TryCreate(instanceUrl, UriKind.Absolute, out _baseAddress))
            {
                _handler = new HttpClientHandler();
                _client = new HttpClient(_handler)
                {
                    BaseAddress = _baseAddress
                };
                _upstreamUserAgent = FormatAssemblyUserAgent(Assembly.GetEntryAssembly());

                //_upstreamUserAgent = FormatAssemblyUserAgent(Assembly.GetEntryAssembly());
                InstanceUrl = _client.BaseAddress.AbsoluteUri;
            }
            else
                throw new Exception("Instance url is not valid.");
        }

        public string InstanceUrl { get; private set; }

        private const string _operation = "{0}/{1}?op={2}";
        public async Task<XDocument> Operation(IV1Asset asset, string operation) //this is higher level so should this live here?
        {
            using (var client = HttpInstance)
            {
                var endPoint = string.Format(_operation, GetResourceUrl(asset.AssetType), asset.ID, operation);

                var response = await client.PostAsync(endPoint, new StringContent(""));
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response);

                return XDocument.Parse(responseContent);
            }
        }

        public async Task<XDocument> Post(IV1Asset asset, XDocument postPayload)
        {
            using (var client = HttpInstance)
            {
                var endPoint = GetResourceUrl(asset.AssetType);
                if (!string.IsNullOrWhiteSpace(asset.ID))
                    endPoint += "/" + asset.ID;

                var response = await client.PostAsync(endPoint, new StringContent(postPayload.ToString()));
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response, postPayload.ToString());

                return XDocument.Parse(responseContent);
            }
        }

        private HttpClientHandler _clientHandler
        {
            get
            {
                return new HttpClientHandler()
                {
                    PreAuthenticate = true,
                    AllowAutoRedirect = true,
                    Credentials = _networkCreds
                };
            }
        }

        private HttpClient HttpInstance
        {
            get
            {
                return new HttpClient(_clientHandler)
                {
                    BaseAddress = _baseAddress
                };
            }
        }

        public async Task<List<T>> Query<T>(string asset, string[] properties, string[] wheres, Func<XElement, T> returnObject)
        {
            var result = new List<T>();

            using (var client = HttpInstance)
            {
                var whereClause = string.Join(";", wheres);

                var endpoint = GetResourceUrl(asset) + "?sel=" + string.Join(",", properties) + "&where=" + whereClause;

                var response = await client.GetAsync(endpoint);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response);

                var doc = XDocument.Parse(responseContent);
                if (doc.HasAssets())
                    result = doc.Root.Elements("Asset").ToList().Select(returnObject.Invoke).ToList();
            }

            return result;
        }

        public async Task<List<T>> Query<T>(string asset, string[] properties, Func<XElement, T> returnObject)
        {
            var result = new List<T>();
            using (var client = HttpInstance)
            {
                var endpoint = GetResourceUrl(asset) + "?sel=" + string.Join(",", properties);

                var response = await client.GetAsync(endpoint);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response);

                var doc = XDocument.Parse(responseContent);
                if (doc.HasAssets())
                    result = doc.Root.Elements("Asset").ToList().Select(returnObject.Invoke).ToList();
            }

            return result;
        }

        public async Task QueryOne(string assetType, string assetId, string[] properties, Action<XElement> returnObject)
        {
            using (var client = HttpInstance)
            {
                var endpoint = GetResourceUrl(assetType + "/" + assetId) + "?sel=" + string.Join(",", properties);

                var response = await client.GetAsync(endpoint);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response);

                var doc = XDocument.Parse(responseContent);
                returnObject.Invoke(doc.Root);
            }
        }

        public async Task<XDocument> Operation(string assetType, string assetId, string operation)
        {
            using (var client = HttpInstance)
            {
                var endpoint = GetResourceUrl(assetType + "/" + assetId) + "?op=" + operation;
                var response = await client.PostAsync(endpoint, new StringContent(""));
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response);

                return XDocument.Parse(responseContent);
            }
        }

        public bool IsConnectionValid()
        {
            using (var client = HttpInstance)
            {
                HttpResponseMessage response;
                try
                {
                    var endpoint = GetResourceUrl("Member") + "?sel=Member.IsSelf";
                    response = client.GetAsync(endpoint).Result;
                }
                catch (Exception)
                {
                    throw new ConfigurationErrorsException("Could not connecto to V1. Bad url.");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new ConfigurationErrorsException("Could not connecto to V1. Bad credentials.");

                return response.IsSuccessStatusCode;
            }
        }

        public bool ProjectExists(string projectIdOrKey)
        {
            var result = Query("Scope", new[] { "Name" }, new[] { string.Format("ID='{0}'", projectIdOrKey) },
                element =>
                {
                    return element.Elements("Attribute").Where(e => e.Attribute("name") != null).Select(e => e.Value);
                }).Result;
            return result.Any();
        }

        public bool EpicCategoryExists(string epicCategoryId)
        {
            var result = Query("EpicCategory", new[] { "Name" }, new[] { string.Format("ID='{0}'", epicCategoryId) },
                element =>
                {
                    return element.Elements("Attribute").Where(e => e.Attribute("name") != null).Select(e => e.Value);
                }).Result;
            return result.Any();
        }

        public static ICanSetUserAgentHeader WithInstanceUrl(string versionOneInstanceUrl)
        {
            return new Builder(versionOneInstanceUrl);
        }

        internal void SetUpstreamUserAgent(string userAgent)
        {
            _upstreamUserAgent = userAgent;
        }

        private string GetResourceUrl(string resource)
        {
            if (string.IsNullOrWhiteSpace(_endpoint))
                throw new ConfigurationErrorsException("V1Connector is not properly configured. The API endpoint was not specified.");

            return _endpoint + ValidateResource(resource);
        }

        private string FormatAssemblyUserAgent(Assembly a, string upstream = null)
        {
            if (a == null) return null;
            var n = a.GetName();
            var s = String.Format("{0}/{1} ({2})", n.Name, n.Version, n.FullName);
            if (!String.IsNullOrEmpty(upstream))
                s = s + " " + upstream;
            return s;
        }

        private string ValidateResource(string resource)
        {
            var result = string.Empty;
            if (resource != null && !resource.StartsWith("/"))
            {
                result = "/" + resource;
            }

            return result;
        }

        private void LogResponse(HttpResponseMessage resp, string rc = "")
        {
            LogRequest(resp.RequestMessage, rc);
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("RESPONSE");
            stringBuilder.AppendLine("\tStatus code: " + resp.StatusCode);
            stringBuilder.AppendLine("\tHeaders: ");
            foreach (var header in resp.Headers)
            {
                stringBuilder.AppendLine("\t\t" + header.Key + "=" + string.Join(", ", header.Value));
            }
            stringBuilder.AppendLine("\tBody: ");
            stringBuilder.AppendLine("\t\t" + (resp.Content != null ? resp.Content.ReadAsStringAsync().Result : string.Empty));

            Log.Trace(stringBuilder.ToString());
        }

        private void LogRequest(HttpRequestMessage rm, string rc)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("REQUEST");
            stringBuilder.AppendLine("\tMethod: " + rm.Method);
            stringBuilder.AppendLine("\tRequest URL: " + rm.RequestUri);
            stringBuilder.AppendLine("\tHeaders: ");
            foreach (var header in rm.Headers)
            {
                stringBuilder.AppendLine("\t\t" + header.Key + "=" + string.Join(", ", header.Value));
            }
            stringBuilder.AppendLine("\tBody: ");
            stringBuilder.AppendLine("\t\t" + rc);

            Log.Trace(stringBuilder.ToString());
        }

        private class Builder : ICanSetUserAgentHeader, ICanSetAuthMethod, ICanSetProxyOrEndpointOrGetConnector, ICanSetEndpointOrGetConnector, ICanSetProxyOrGetConnector
        {
            private readonly V1Connector _instance;

            public Builder(string versionOneInstanceUrl)
            {
                _instance = new V1Connector(versionOneInstanceUrl);
            }

            public ICanSetAuthMethod WithUserAgentHeader(string name, string version)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentNullException("name");
                if (string.IsNullOrWhiteSpace(version))
                    throw new ArgumentNullException("version");

                _instance._client.DefaultRequestHeaders.Add(name, version);

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithUsernameAndPassword(string username, string password)
            {
                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentNullException("username");
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentNullException("password");

                _instance._handler.Credentials = new NetworkCredential(username, password);
                _networkCreds = _instance._handler.Credentials;
                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithWindowsIntegrated()
            {
                var credentialCache = new CredentialCache
                {
                    {_instance._client.BaseAddress, "NTLM", CredentialCache.DefaultNetworkCredentials},
                    {_instance._client.BaseAddress, "Negotiate", CredentialCache.DefaultNetworkCredentials}
                };
                _instance._handler.Credentials = credentialCache;
                _networkCreds = _instance._handler.Credentials;

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithWindowsIntegrated(string fullyQualifiedDomainUsername, string password)
            {
                if (string.IsNullOrWhiteSpace(fullyQualifiedDomainUsername))
                    throw new ArgumentNullException("fullyQualifiedDomainUsername");
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentNullException("password");

                _instance._handler.Credentials = new NetworkCredential(fullyQualifiedDomainUsername, password);
                _networkCreds = _instance._handler.Credentials;

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithAccessToken(string accessToken)
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                    throw new ArgumentNullException("accessToken");

                _instance._client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithOAuth2Token(string accessToken)
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                    throw new ArgumentNullException("accessToken");

                _instance._client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                return this;
            }

            public ICanSetProxyOrGetConnector UseEndpoint(string endpoint)
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                    throw new ArgumentNullException("endpoint");

                _instance._endpoint = endpoint;

                return this;
            }

            public ICanSetEndpointOrGetConnector WithProxy(ProxyProvider proxyProvider)
            {
                if (proxyProvider == null)
                    throw new ArgumentNullException("proxyProvider");

                _instance._handler.Proxy = proxyProvider.CreateWebProxy();

                return this;
            }

            public V1Connector Build()
            {
                return _instance;
            }

            ICanGetConnector ICanSetProxyOrGetConnector.WithProxy(ProxyProvider proxyProvider)
            {
                if (proxyProvider == null)
                    throw new ArgumentNullException("proxyProvider");

                _instance._handler.Proxy = proxyProvider.CreateWebProxy();

                return this;
            }

            ICanGetConnector ICanSetEndpointOrGetConnector.UseEndpoint(string endpoint)
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                    throw new ArgumentNullException("endpoint");

                _instance._endpoint = endpoint;

                return this;
            }
        }

    }
}
