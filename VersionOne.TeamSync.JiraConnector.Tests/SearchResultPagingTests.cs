using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using log4net;
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;
using Should;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.JiraConnector.Tests
{
    [TestClass]
    public class SearchResult_more_than_1000_results_Tests
    {
        private List<IRestRequest> _restRequest = new List<IRestRequest>();
        private Mock<IRestResponse>[] _restResponse;
        private Mock<IV1Log> _mockLog;
        private Mock<IRestClient> _restClient;
        private int callNumber = 0;

        private string _overloadedResponse = "{\"startAt\":0,\"maxResults\":1000,\"total\":1001,\"issues\":[{\"id\":5 }]}";
        private string _secondOverLoadResponse = "{\"startAt\":1000,\"maxResults\":1000,\"total\":1001,\"issues\":[{\"id\":6 }]}";

        private SearchResult _result;
        private IRestRequest _firstRequest;
        private IRestRequest _secondRequest;

        private Connector.JiraConnector CreateConnect()
        {
            _restClient = new Mock<IRestClient>();
            _restResponse = new[]
            {
                MakeAResponse(HttpStatusCode.OK, _overloadedResponse),
                MakeAResponse(HttpStatusCode.OK, _secondOverLoadResponse)
            };
            _restClient.Setup(x => x.BaseUrl).Returns(new Uri("http://baseUrl"));
            _restClient.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns((IRestRequest request) =>
            {
                var returnObject = _restResponse[callNumber].Object;
                _restRequest.Add(request);
                callNumber++;
                return returnObject;
            });

            _mockLog = new Mock<IV1Log>();
            var mockLoggerFactory = new Mock<IV1LogFactory>();
            mockLoggerFactory.Setup(x => x.Create<Connector.JiraConnector>()).Returns(_mockLog.Object);

            return new Connector.JiraConnector(_restClient.Object, mockLoggerFactory.Object);
        }

        private Mock<IRestResponse> MakeAResponse(HttpStatusCode expectedCode, string content)
        {
            var response = new Mock<IRestResponse>();
            response.Setup(x => x.StatusCode).Returns(expectedCode);
            response.Setup(x => x.Content).Returns(content);
            response.Setup(x => x.Headers).Returns(new List<Parameter>());
            return response;
        }

        [TestInitialize]
        public void Context()
        {
            var connector = CreateConnect();

            _result = connector.GetAllSearchResults(new Dictionary<string, IEnumerable<string>>
            {
                {"project",new[]{"WAT", "HUH"}},
                {"stuff",new[]{"whos-there"}}
            }, 
                new[] { "lulz" }
            );

            _firstRequest = _restRequest.First();
            _secondRequest = _restRequest.Last();
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

        [TestMethod]
        public void should_have_the_two_different_Ids_for_issue()
        {
            _result.issues.First().id.ShouldEqual("5");
            _result.issues.Last().id.ShouldEqual("6");
        }

        [TestMethod]
        public void should_log_a_message_that_theres_a_buncha_stuff()
        {
            _mockLog.Verify(x => x.Info("More than 1000 results found ... gathering up all 1001"), Times.Once);
        }

        [TestMethod]
        public void first_request_should_have_jql_with_a_project_of_WAT()
        {
            _firstRequest.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("project in (WAT, HUH) AND stuff=whos-there");
        }

        [TestMethod]
        public void first_request_should_have_maxResults_number_of_1000()
        {
            _firstRequest.Parameters.Single(x => x.Name == "maxResults").Value.ShouldEqual("1000");
        }

        [TestMethod]
        public void first_request_should_have_fields_of_lulz()
        {
            _firstRequest.Parameters.Single(x => x.Name == "fields").Value.ShouldEqual("lulz");
        }

        [TestMethod]
        public void second_request_should_have_jql_with_a_project_of_WAT()
        {
            _secondRequest.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("project in (WAT, HUH) AND stuff=whos-there");
        }

        [TestMethod]
        public void second_request_should_have_maxResults_number_of_1000()
        {
            _secondRequest.Parameters.Single(x => x.Name == "maxResults").Value.ShouldEqual("1000");
        }

        [TestMethod]
        public void second_request_should_have_fields_of_lulz()
        {
            _secondRequest.Parameters.Single(x => x.Name == "fields").Value.ShouldEqual("lulz");
        }

        [TestMethod]
        public void second_request_should_have_a_startAt_of_1001()
        {
            _secondRequest.Parameters.Single(x => x.Name == "startAt").Value.ShouldEqual("1001");
        }
    }


    [TestClass]
    public class SearchResult_1000_or_less_results_Tests
    {
        private Mock<IRestResponse> _restResponse;
        private Mock<IV1Log> _mockLog;
        private Mock<IRestClient> _restClient;

        private string _overloadedResponse = "{\"startAt\":0,\"maxResults\":1000,\"total\":1000,\"issues\":[{\"id\":5 }]}";

        private SearchResult _result;
        private IRestRequest _request;

        private Connector.JiraConnector CreateConnect()
        {
            _restClient = new Mock<IRestClient>();
            _restResponse = MakeAResponse(HttpStatusCode.OK, _overloadedResponse);
            
            _restClient.Setup(x => x.BaseUrl).Returns(new Uri("http://baseUrl"));
            _restClient.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns((IRestRequest request) =>
            {
                _request = request;
                return _restResponse.Object;
            });

            _mockLog = new Mock<IV1Log>();
            var mockLoggerFactory = new Mock<IV1LogFactory>();
            mockLoggerFactory.Setup(x => x.Create<Connector.JiraConnector>()).Returns(_mockLog.Object);

            return new Connector.JiraConnector(_restClient.Object, mockLoggerFactory.Object);
        }

        private Mock<IRestResponse> MakeAResponse(HttpStatusCode expectedCode, string content)
        {
            var response = new Mock<IRestResponse>();
            response.Setup(x => x.StatusCode).Returns(expectedCode);
            response.Setup(x => x.Content).Returns(content);
            response.Setup(x => x.Headers).Returns(new List<Parameter>());
            return response;
        }

        [TestInitialize]
        public void Context()
        {
            var connector = CreateConnect();

            _result = connector.GetAllSearchResults(new Dictionary<string, IEnumerable<string>>
            {
                {"project",new[]{"WAT", "HUH"}},
                {"stuff",new[]{"whos-there"}}
            },
                new[] { "lulz" }
            );
        }

        [TestMethod]
        public void should_have_called_get_twice()
        {
            _restClient.Verify(x => x.Execute(It.IsAny<IRestRequest>()), Times.Once);
        }

        [TestMethod]
        public void should_have_1_issues()
        {
            _result.issues.Count.ShouldEqual(1);
        }

        [TestMethod]
        public void should_have_the_one_id_for_issue()
        {
            _result.issues.Single().id.ShouldEqual("5");
        }

        [TestMethod]
        public void should_not_log_a_message_that_theres_a_buncha_stuff()
        {
            _mockLog.Verify(x => x.Info(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void request_should_have_jql_with_a_project_of_WAT()
        {
            _request.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("project in (WAT, HUH) AND stuff=whos-there");
        }

        [TestMethod]
        public void request_should_have_maxResults_number_of_1000()
        {
            _request.Parameters.Single(x => x.Name == "maxResults").Value.ShouldEqual("1000");
        }

        [TestMethod]
        public void request_should_have_fields_of_lulz()
        {
            _request.Parameters.Single(x => x.Name == "fields").Value.ShouldEqual("lulz");
        }
    }

}
