using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using log4net;
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.JiraConnector.Tests
{
    [TestClass]
    public class SearchResultPagingTests
    {
        private Mock<IRestRequest> _restRequest;
        private Mock<IRestResponse> _restResponse;
        private string _basicSearchPayload = "{\"startAt\":0,\"maxResults\":1000,\"total\":1001,\"issues\":[{\"id\":5 }]}";
        private Mock<ILog> _mockLog;

        private SearchResult _result;
        private Mock<IRestClient> _restClient;

        private Connector.JiraConnector CreateConnect(HttpStatusCode toReturn)
        {
            _restClient = new Mock<IRestClient>();
            _restRequest = new Mock<IRestRequest>();
            _restResponse = new Mock<IRestResponse>();

            _restRequest.Setup(x => x.Method).Returns(Method.GET);
            _restRequest.Setup(x => x.Parameters).Returns(new List<Parameter>());

            _restResponse.Setup(x => x.StatusCode).Returns(toReturn);
            _restResponse.Setup(x => x.Content).Returns(_basicSearchPayload);
            _restResponse.Setup(x => x.Headers).Returns(new List<Parameter>());
            _restClient.Setup(x => x.BaseUrl).Returns(new Uri("http://baseUrl"));
            _restClient.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns(_restResponse.Object);

            _mockLog = new Mock<ILog>();
            _mockLog.SetupGet(x => x.Logger).Returns(new Mock<ILogger>().Object);

            return new JiraConnector.Connector.JiraConnector(_restClient.Object, _mockLog.Object);
        }

        [TestInitialize]
        public void Context()
        {
            var connector = CreateConnect(HttpStatusCode.OK);

            _result = connector.GetAllSearchResults(new Dictionary<string, IEnumerable<string>>(), new[]{""});
        }

        [TestMethod]
        public void should_have_called_get_twice()
        {
            _restClient.Verify(x => x.Execute(It.IsAny<IRestRequest>()), Times.Exactly(2));
        }

        [TestMethod]
        public void should_have_2_issues()
        {
            _result.issues.Count.ShouldEqual(2);
        }
    }
}
