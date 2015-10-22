using System.Collections.Generic;
using System.Net;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraConnector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{

    [TestClass]
    public class Jira_SettingProjectToResoved_Tests
    {
        private Mock<IJiraConnector> _mockConnector;
        private Mock<ILog> _mockLogger;
        private const string IssueKey = "AKey-10";

        [TestInitialize]
        public void Context()
        {
            _mockLogger = new Mock<ILog>();
            _mockLogger.Setup(x => x.Error(It.IsAny<string>()));
            _mockConnector = new Mock<IJiraConnector>();
            _mockConnector.Setup(x => x.Get<TransitionResponse>("api/latest/issue/{issueIdOrKey}/transitions", new KeyValuePair<string, string>("issueIdOrKey", IssueKey), It.IsAny<Dictionary<string, string>>())).Returns(new TransitionResponse
            {
                Transitions = new List<Transition> { new Transition { Id = "5", Name = "Done" } }
            }).Verifiable();
            _mockConnector.Setup(x => x.Post("api/latest/issue/{issueIdOrKey}/transitions", It.IsAny<object>(), HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", IssueKey))).Verifiable();

            var jira = new Jira(_mockConnector.Object, null, _mockLogger.Object);

            jira.SetIssueToResolved(IssueKey, new[] { "Done" });
        }

        [TestMethod]
        public void all_mocked_calls_are_called() //I don't think I like this...
        {
            _mockConnector.VerifyAll();
        }

        [TestMethod]
        public void logs_a_message_saying_it_set_the_status()
        {
            _mockLogger.Verify(x => x.Info("Set status on AKey-10 to Done"));
        }
    }
}
