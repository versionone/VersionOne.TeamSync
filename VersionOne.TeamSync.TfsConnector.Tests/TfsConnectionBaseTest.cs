using System.Collections.Generic;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.Interfaces.RestClient;
using VersionOne.TeamSync.TfsConnector.Config;

namespace VersionOne.TeamSync.TfsConnector.Tests
{
    public class TfsConnectionBaseTest
    {
        protected TfsServer Server = new TfsServer { };
        protected Mock<IV1LogFactory> MockLogFactory = new Mock<IV1LogFactory>();

        protected Mock<ITeamSyncRestClientFactory> MockRestClientFactory = new Mock<ITeamSyncRestClientFactory>();
        protected Mock<ITeamSyncRestClient> MockRestClient = new Mock<ITeamSyncRestClient>();
        protected Mock<ITeamSyncRestResponse> MockRestResponse = new Mock<ITeamSyncRestResponse>();
        protected Connector.TfsConnector SUT;

        public void SetupConnector(string path)
        {
            MockRestClientFactory.Setup(m => m.Create(It.IsAny<TeamSyncRestClientSettings>())).Returns(MockRestClient.Object);
            MockRestClient.Setup(m => m.Execute(path,
                It.IsAny<KeyValuePair<string, string>>(), It.IsAny<IDictionary<string, string>>()))
                .Returns(MockRestResponse.Object);
            SUT = new Connector.TfsConnector(Server, MockLogFactory.Object, MockRestClientFactory.Object);
        }

        protected void SetupRestResponse(HttpStatusCode statusCodeToReturn, string errorMessage = "")
        {
            MockRestResponse.Setup(m => m.StatusCode).Returns(statusCodeToReturn);
            if (!string.IsNullOrWhiteSpace(errorMessage)) MockRestResponse.Setup(m => m.ErrorMessage).Returns(errorMessage);
        }
    }
}