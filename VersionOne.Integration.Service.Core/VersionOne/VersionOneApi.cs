using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersionOne.Integration.Service.Core.VersionOne.Interfaces;
using VersionOne.Integration.Service.Core.VersionOne.Xml;

namespace VersionOne.Integration.Service.Core.VersionOne
{
	public class InstanceInfo
	{
		public string Host { get; private set; }
		public string Instance { get; private set; }
	}

	public class VersionOneApi
	{
		private string _base64;
		private DebugHttpClientHandler _debugHttp;
		private const string host = "http://localhost";
		private const string instance = "VersionOne";
		public VersionOneApi()
		{
			var nom = Encoding.UTF8.GetBytes("admin:admin");
			_base64 = Convert.ToBase64String(nom);
			_debugHttp = new DebugHttpClientHandler();
		}

		public async Task<XDocument> Post(IV1Asset asset)
		{
			var xmlPayload = asset.ToPostPayload();
			
			using (var client = new HttpClient())
			{
				SetBasics(client);

				var endPoint = "/VersionOne/rest-1.v1/Data/" + asset.AssetType;
				if (!string.IsNullOrWhiteSpace(asset.ID))
					endPoint += "/" + asset.ID;

				var response = await client.PostAsync(endPoint, new StringContent(xmlPayload.ToString()));
				var value = await response.Content.ReadAsStringAsync();
				return XDocument.Parse(value);
			}

		}

		public async Task<List<T>> Query<T>(string asset, string[] properties, string[] wheres, Func<XElement, T> returnObject)
		{
			var result = new List<T>();

			using (var client = new HttpClient())
			{
				SetBasics(client);
				var whereClause = string.Join(";", wheres);

				var hardCode = "/VersionOne/rest-1.v1/Data/" + asset + "?sel=" + string.Join(",", properties) + "&" + whereClause;

				var xml = await client.GetStringAsync(hardCode);
				var doc = XDocument.Parse(xml);
				if (doc.HasAssets())
					result = doc.Root.Elements("Asset").ToList().Select(returnObject.Invoke).ToList();
			}

			return result;
		}

		public async Task<List<T>> Query<T>(string asset, string[] properties, Func<XElement, T> returnObject)
		{
			var result = new List<T>();

			using (var client = new HttpClient())
			{
				SetBasics(client);

				var hardCode = "/VersionOne/rest-1.v1/Data/" + asset + "?sel=" + string.Join(",", properties);
				//var uri = "/rest-1.v1/Data/" + asset + "?sel=" + string.Join(",", properties);

				var xml = await client.GetStringAsync(hardCode);
				var doc = XDocument.Parse(xml);
				if (doc.HasAssets())
					result = doc.Root.Elements("Asset").ToList().Select(returnObject.Invoke).ToList();
			}

			return result;
		}

		private void SetBasics(HttpClient client)
		{
			client.BaseAddress = new Uri(host);
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _base64);
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
		}
	}


	public class DebugHttpClientHandler : HttpClientHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			//debugger guy thing
			var item = request.RequestUri;

			return base.SendAsync(request, cancellationToken);
		}
	}
}