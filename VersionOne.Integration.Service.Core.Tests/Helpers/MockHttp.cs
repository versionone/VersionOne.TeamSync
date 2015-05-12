using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VersionOne.Integration.Service.Worker.Tests.Helpers
{
	public class MockHttp : HttpMessageHandler
	{
		public MockHttp()
		{
			RequestMessage = new List<HttpRequestMessage>();
			ResponseMessage = new Dictionary<string, HttpResponseMessage>();
		}

		public List<HttpRequestMessage> RequestMessage { get; private set; }
		public Dictionary<string, HttpResponseMessage> ResponseMessage { get; private set; }
		
		public void AddResponse(string url, HttpResponseMessage response)
		{
			ResponseMessage.Add(url, response);
		}

		public void AddExpectedUrlAndSetJsonResponse(string expectedUrl, string jsonResponse)
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
			};

			ResponseMessage.Add(expectedUrl, response);
		}

		public void AddExpectedUrlAndSetXmlResponse(string expectedUrl, string xmlResponse, HttpStatusCode httpStatusCode)
		{
			var response = new HttpResponseMessage(httpStatusCode)
			{
				Content = new StringContent(xmlResponse, Encoding.UTF8, "application/xml")
			};

			ResponseMessage.Add(expectedUrl, response);
		}


		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestMessage.Add(request);
			var requestUri = request.RequestUri.ToString();
			if (ResponseMessage.ContainsKey(requestUri))
				return Task.FromResult(ResponseMessage[requestUri]);

			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
			{
				Content = new StringContent("Uri was " + request.RequestUri)
			});
		}
	}
}



