using System;
using System.Net;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Deserializers;
using VersionOne.TeamSync.JiraConnector.Config;

namespace VersionOne.TeamSync.LoadTester
{
    public class JiraRestService
    {
        private readonly IRestClient _client;

        public JiraRestService(JiraServer settings)
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

            _client = new RestClient(new Uri(new Uri(settings.Url), "/rest").ToString()) { Proxy = proxy };

            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            {
                _client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);
            }

            if (settings.IgnoreCertificate)
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, errors) => true;
        }

        public string Post(string path, object data)
        {
            var request = new RestRequest
            {
                Method = Method.POST,
                Resource = path,
                RequestFormat = DataFormat.Json,
            };
            request.AddBody(data);

            dynamic resp = JObject.Parse(_client.Execute(request).Content);
            
            return resp.key;
        }
    }
}
