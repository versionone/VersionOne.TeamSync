using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.TfsConnector.Config;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.Interfaces.RestClient;
using System.Net;
using VersionOne.TeamSync.TfsConnector.Exceptions;

namespace VersionOne.TeamSync.TfsConnector.Tests
{
	public class TFS_connection_base_test
	{
		protected TfsServer Server = new TfsServer { };
		protected Mock<IV1LogFactory> MockLogFactory = new Mock<IV1LogFactory>();

		protected Mock<ITeamSyncRestClientFactory> MockRestClientFactory = new Mock<ITeamSyncRestClientFactory>();
		protected Mock<ITeamSyncRestClient> MockRestClient = new Mock<ITeamSyncRestClient>();
		protected Mock<ITeamSyncRestResponse> MockRestResponse = new Mock<ITeamSyncRestResponse>();
		protected Connector.TfsConnector SUT;

		[TestInitialize]
		public void Setup()
		{
			MockRestClientFactory.Setup(m => m.Create(It.IsAny<TeamSyncRestClientSettings>())).Returns(MockRestClient.Object);
			MockRestClient.Setup(m => m.Execute("/tfs/DefaultCollection/_apis/projects?api-version=1.0&$top=1",
				It.IsAny<KeyValuePair<string, string>>(), It.IsAny<IDictionary<string, string>>()))
				.Returns(MockRestResponse.Object);
			SUT = new Connector.TfsConnector(Server, MockLogFactory.Object, MockRestClientFactory.Object);
		}

		protected void SetupRestResponse(HttpStatusCode statusCodeToReturn, string errorMessage = "")
		{
			MockRestResponse.Setup(m => m.StatusCode).Returns(statusCodeToReturn);
			if (!string.IsNullOrWhiteSpace(errorMessage)) MockRestResponse.Setup(m => m.ErrorMessage).Returns(errorMessage);
		}

		[TestClass]
		public class TFS_allows_connection_and_returns_OK : TFS_connection_base_test
		{
			[TestInitialize]
			public void Context()
			{
				SetupRestResponse(HttpStatusCode.OK);
			}

			[TestMethod]
			public void IsConnectionValid_should_return_true()
			{
				SUT.IsConnectionValid().ShouldBeTrue();
			}
		}

		[TestClass]
		public class TFS_allows_connection_but_returns_an_error : TFS_connection_base_test
		{
			private readonly string ErrorMessage = "This is some crazy error!";
			private TfsException ThrownException;

			[TestInitialize]
			public void Context()
			{
				SetupRestResponse(HttpStatusCode.BadRequest, ErrorMessage);
				try
				{
					SUT.IsConnectionValid();
				}
				catch (TfsException exception)
				{
					ThrownException = exception;
				}
			}

			[TestMethod]
            public void ThrownException_is_of_type_TfsException()
			{
                ThrownException.ShouldBeType<TfsException>();
			}

			[TestMethod]
			public void TfsException_Message_should_be_generic()
			{
				ThrownException.Message.ShouldEqual("Could not connect to TFS.");
			}

			[TestMethod]
			public void TfsException_contains_an_InnerException()
			{
				ThrownException.InnerException.ShouldNotBeNull();
			}

			[TestMethod]
			public void TfsException_InnerException_Message_should_be_TFS_response_ErrorMessage()
			{
				ThrownException.InnerException.Message.ShouldEqual(ErrorMessage);
			}
		}
		
		[TestClass]
        public class TFS_rejects_connection : TFS_connection_base_test
		{

            private TfsException ThrownException;

            [TestInitialize]
            public void Context()
            {
                SetupRestResponse(HttpStatusCode.Unauthorized);

                try
                {
                    SUT.IsConnectionValid();
                }
                catch (TfsException exception)
                {
                    ThrownException = exception;
                }
            }

            [TestMethod]
            public void ThrownException_is_of_type_TfsLoginException()
            {
                ThrownException.ShouldBeType<TfsLoginException>();
            }

            [TestMethod]
            public void ThrownException_Message_should_be_specific()
            {
                ThrownException.Message.ShouldEqual("Could not connect to TFS. Bad credentials.");
            }

		}
	}
}
