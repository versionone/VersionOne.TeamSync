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
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.V1Connector.Extensions;

namespace VersionOne.TeamSync.V1Connector
{
    public class V1Connector : IV1Connector
    {
        private const string DATA_API_OAUTH_ENDPOINT = "rest-1.oauth.v1/Data";
        private const string DATA_API_ENDPOINT = "rest-1.v1/Data";
        private const string QUERY_STRING_OPERATION = "{0}/{1}?op={2}";

        private readonly Uri _baseAddress;
        private ICredentials _networkCreds;
        private readonly IDictionary<string, string> _requestHeaders = new Dictionary<string, string>();
        private bool _useOAuthEndpoints;
        private IWebProxy _proxy;
        private IV1Log _log;

        public V1Connector(string instanceUrl)
        {
            if (string.IsNullOrWhiteSpace(instanceUrl))
                throw new ArgumentNullException("instanceUrl");
            if (!instanceUrl.EndsWith("/"))
                instanceUrl += "/";

            if (Uri.TryCreate(instanceUrl, UriKind.Absolute, out _baseAddress))
            {
                InstanceUrl = _baseAddress.AbsoluteUri;
            }
            else
                throw new Exception("Instance url is not valid.");
        }

        public string InstanceUrl { get; private set; }

        public string MemberId { get; private set; }

        public async Task<XDocument> Operation(IV1Asset asset, string operation) //this is higher level so should this live here?
        {
            using (var client = HttpInstance)
            {
                var endPoint = string.Format(QUERY_STRING_OPERATION, GetResourceUrl(asset.AssetType), asset.ID, operation);

                var response = await client.PostAsync(endPoint, new StringContent(""));
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response);

                return XDocument.Parse(responseContent);
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

        public async Task<XDocument> Post(IV1Asset asset, XDocument postPayload)
        {
            using (var client = HttpInstance)
            {
                var endpoint = GetResourceUrl(asset.AssetType);
                if (!string.IsNullOrWhiteSpace(asset.ID))
                    endpoint += "/" + asset.ID;

                var response = await client.PostAsync(endpoint, new StringContent(postPayload.ToString()));
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response, postPayload.ToString());

                return XDocument.Parse(responseContent);
            }
        }

        public async Task<XDocument> Post(string resource, string postPayload)
        {
            using (var client = HttpInstance)
            {
                var endpoint = GetResourceUrl(resource);
                var response = await client.PostAsync(endpoint, new StringContent(postPayload));
                var responseContent = await response.Content.ReadAsStringAsync();

                LogResponse(response, postPayload);

                return XDocument.Parse(responseContent);
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

        public bool IsConnectionValid()
        {
            using (var client = HttpInstance)
            {
                var endpoint = GetResourceUrl("Member") + "?sel=IsSelf,ID&where=IsSelf='true'";
                var response = client.GetAsync(endpoint).Result;

                LogResponse(response);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    var doc = XElement.Parse(responseContent);
                    var member = doc.Descendants("Asset").FirstOrDefault();
                    if (member != null)
                        MemberId = member.Attribute("id").Value;
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new ConfigurationErrorsException("Could not connect to V1. Bad credentials.");

                return response.IsSuccessStatusCode;
            }
        }

        public bool AssetFieldExists(string asset, string field)
        {
            using (var client = HttpInstance)
            {
                var endpoint = string.Format("meta.v1/{0}/{1}", asset, field);

                var response = client.GetAsync(endpoint).Result;

                LogResponse(response);

                return response.IsSuccessStatusCode;
            }
        }

        public static ICanSetUserAgentHeader WithInstanceUrl(string versionOneInstanceUrl)
        {
            return new Builder(versionOneInstanceUrl);
        }

        private HttpClientHandler ClientHandler
        {
            get
            {
                return new HttpClientHandler
                {
                    PreAuthenticate = true,
                    AllowAutoRedirect = true,
                    Credentials = _networkCreds,
                    Proxy = _proxy
                };
            }
        }

        private HttpClient HttpInstance
        {
            get
            {
                var httpInstance = new HttpClient(ClientHandler)
                {
                    BaseAddress = _baseAddress
                };
                foreach (var requestHeader in _requestHeaders)
                {
                    httpInstance.DefaultRequestHeaders.Add(requestHeader.Key, requestHeader.Value);
                }

                return httpInstance;
            }
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

        private string GetResourceUrl(string resource)
        {
            if (string.IsNullOrWhiteSpace(GetEndpoint()))
                throw new ConfigurationErrorsException("V1Connector is not properly configured. The API endpoint was not specified.");

            return GetEndpoint() + ValidateResource(resource);
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

            _log.Trace(stringBuilder.ToString());
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

            _log.Trace(stringBuilder.ToString());
        }

        private string GetEndpoint()
        {
            return _useOAuthEndpoints ? DATA_API_OAUTH_ENDPOINT : DATA_API_ENDPOINT;
        }

        public class Builder : ICanSetUserAgentHeader, ICanSetAuthMethod, ICanSetProxyOrEndpointOrGetConnector, ICanSetProxyOrGetConnector
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

                _instance._requestHeaders.Add(name, version);

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithUsernameAndPassword(string username, string password)
            {
                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentNullException("username");
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentNullException("password");

                _instance._networkCreds = new NetworkCredential(username, password);

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithWindowsIntegrated()
            {
                var credentialCache = new CredentialCache
                {
                    {_instance._baseAddress, "NTLM", CredentialCache.DefaultNetworkCredentials},
                    {_instance._baseAddress, "Negotiate", CredentialCache.DefaultNetworkCredentials}
                };
                _instance._networkCreds = credentialCache;

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithWindowsIntegrated(string fullyQualifiedDomainUsername, string password)
            {
                if (string.IsNullOrWhiteSpace(fullyQualifiedDomainUsername))
                    throw new ArgumentNullException("fullyQualifiedDomainUsername");
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentNullException("password");

                _instance._networkCreds = new NetworkCredential(fullyQualifiedDomainUsername, password);

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithAccessToken(string accessToken)
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                    throw new ArgumentNullException("accessToken");

                _instance._requestHeaders.Add("Authorization", "Bearer " + accessToken);

                return this;
            }

            public ICanSetProxyOrEndpointOrGetConnector WithOAuth2Token(string accessToken)
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                    throw new ArgumentNullException("accessToken");

                _instance._requestHeaders.Add("Authorization", "Bearer " + accessToken);

                return this;
            }

            public ICanGetConnector WithProxy(IProxyProvider proxyProvider)
            {
                if (proxyProvider == null)
                    throw new ArgumentNullException("proxyProvider");

                _instance._proxy = proxyProvider.CreateWebProxy();

                return this;
            }

            public IV1Connector Build(IV1LogFactory v1LogFactory)
            {
                _instance._log = v1LogFactory.Create<V1Connector>();
                return _instance;
            }

            ICanGetConnector ICanSetProxyOrGetConnector.WithProxy(IProxyProvider proxyProvider)
            {
                if (proxyProvider == null)
                    throw new ArgumentNullException("proxyProvider");

                _instance._proxy = proxyProvider.CreateWebProxy();

                return this;
            }

            public ICanSetProxyOrGetConnector UseOAuthEndpoints()
            {
                _instance._useOAuthEndpoints = true;

                return this;
            }
        }
    }
}
