using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.TfsConnector.Exceptions;

namespace VersionOne.TeamSync.TfsConnector.Tests
{
    public class ProjectTests
    {
        [TestClass]
        public class TFS_find_project_and_returns_true : TfsConnectionBaseTest
        {
            [TestInitialize]
            public void Context()
            {
                SetupConnector("/tfs/DefaultCollection/_apis/projects/PROJECT_NAME?api-version=1.0");
                SetupRestResponse(HttpStatusCode.OK);
            }

            [TestMethod]
            public void ProjectExists_should_return_true()
            {
                SUT.ProjectExists("PROJECT_NAME").ShouldBeTrue();
            }
        }

        [TestClass]
        public class TFS_does_not_find_project_and_returns_false : TfsConnectionBaseTest
        {
            [TestInitialize]
            public void Context()
            {
                SetupConnector("/tfs/DefaultCollection/_apis/projects/PROJECT_NAME?api-version=1.0");
                SetupRestResponse(HttpStatusCode.NotFound);
            }

            [TestMethod]
            public void ProjectExists_should_return_false()
            {
                SUT.ProjectExists("PROJECT_NAME").ShouldBeFalse();
            }
        }

        [TestClass]
        public class TFS_returns_BAD_REQUEST_and_throws_an_exception : TfsConnectionBaseTest
        {
            private TfsException ThrownException;
            private readonly string ErrorMessage = "This is some crazy error!";
            private readonly string InnerErrorMessage = "This is another crazy error!";

            [TestInitialize]
            public void Context()
            {
                var path = "/tfs/DefaultCollection/_apis/projects/PROJECT_NAME?api-version=1.0";
                SetupConnector(path);
                MockRestClient.Setup(
                    m =>
                        m.Execute(path, It.IsAny<KeyValuePair<string, string>>(),
                            It.IsAny<IDictionary<string, string>>()))
                    .Throws(new TfsException(HttpStatusCode.BadRequest, ErrorMessage, new Exception(InnerErrorMessage)));
                try
                {
                    SUT.ProjectExists("PROJECT_NAME");
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
            public void TfsException_Message_should_be_ErrorMessage()
            {
                ThrownException.Message.ShouldEqual(ErrorMessage);
            }

            [TestMethod]
            public void TfsException_contains_an_InnerException()
            {
                ThrownException.InnerException.ShouldNotBeNull();
            }

            [TestMethod]
            public void TfsException_InnerException_Message_should_be_InnerErrorMessage()
            {
                ThrownException.InnerException.Message.ShouldEqual(InnerErrorMessage);
            }
        }

        [TestClass]
        public class TFS_returns_NOT_FOUND_and_returns_false : TfsConnectionBaseTest
        {
            private TfsException ThrownException;
            private readonly string ErrorMessage = "This is some crazy error!";
            private readonly string InnerErrorMessage = "This is another crazy error!";

            [TestInitialize]
            public void Context()
            {
                var path = "/tfs/DefaultCollection/_apis/projects/PROJECT_NAME?api-version=1.0";
                SetupConnector(path);
                MockRestClient.Setup(
                    m =>
                        m.Execute(path, It.IsAny<KeyValuePair<string, string>>(),
                            It.IsAny<IDictionary<string, string>>()))
                    .Throws(new TfsException(HttpStatusCode.NotFound, ErrorMessage, new Exception(InnerErrorMessage)));
                try
                {
                    SUT.ProjectExists("PROJECT_NAME");
                }
                catch (TfsException exception)
                {
                    ThrownException = exception;
                }
            }

            [TestMethod]
            public void ProjectExistss_should_not_throw_an_exception()
            {
                ThrownException.ShouldBeNull();
            }

            [TestMethod]
            public void ProjectExists_should_return_false()
            {
                SUT.ProjectExists("PROJECT_NAME").ShouldBeFalse();
            }
        }
    }
}