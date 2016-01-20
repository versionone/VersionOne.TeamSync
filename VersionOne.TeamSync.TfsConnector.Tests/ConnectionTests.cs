using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using System.Net;
using VersionOne.TeamSync.TfsConnector.Exceptions;

namespace VersionOne.TeamSync.TfsConnector.Tests
{
    [TestClass]
    public class TFS_allows_connection_and_returns_OK : TfsConnectionBaseTest
    {
        [TestInitialize]
        public void Context()
        {
            SetupConnector("/tfs/DefaultCollection/_apis/projects?api-version=1.0&$top=1");
            SetupRestResponse(HttpStatusCode.OK);
        }

        [TestMethod]
        public void IsConnectionValid_should_return_true()
        {
            SUT.IsConnectionValid().ShouldBeTrue();
        }
    }

    [TestClass]
    public class TFS_allows_connection_but_returns_an_error : TfsConnectionBaseTest
    {
        private readonly string ErrorMessage = "This is some crazy error!";
        private TfsException ThrownException;

        [TestInitialize]
        public void Context()
        {
            SetupConnector("/tfs/DefaultCollection/_apis/projects?api-version=1.0&$top=1");
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
    public class TFS_rejects_connection : TfsConnectionBaseTest
    {

        private TfsException ThrownException;

        [TestInitialize]
        public void Context()
        {
            SetupConnector("/tfs/DefaultCollection/_apis/projects?api-version=1.0&$top=1");
            SetupRestResponse(HttpStatusCode.Unauthorized);

            try
            {
                SUT.IsConnectionValid();
            }
            catch (TfsLoginException exception)
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
