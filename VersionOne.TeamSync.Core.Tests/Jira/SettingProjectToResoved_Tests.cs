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
    public class Jira_SettingProjectToSomethingThatDoesntExist_Tests : ResolovedSetupContext
    {
        [TestInitialize]
        public override void Context()
        {
            _returnTransition = new Transition { Id = "5", Name = "Not supported status" };
            base.Context();
            RunIt();
        }

        [TestMethod]
        public void does_not_log_a_message_saying_it_set_the_status()
        {
            _mockLogger.Verify(x => x.Info("Set status on AKey-10 to Done"), Times.Never);
        }

        [TestMethod]
        public void gives_an_error_message_that_says_it_failed_moving_to_unsupported_statuses()
        {
            _mockLogger.Verify(x => x.Error("None or multiple transistions exists for AKey-10 with the status of Done.  This epic will not be updated"), Times.Once);
        }
    }

    public abstract class ResolovedSetupContext
    {
        protected Mock<IJiraConnector> _mockConnector = new Mock<IJiraConnector>();
        protected Mock<ILog> _mockLogger = new Mock<ILog>();
        protected const string IssueKey = "AKey-10";
        protected Transition _returnTransition = new Transition();

        public virtual void Context()
        {
            _mockLogger.Setup(x => x.Error(It.IsAny<string>()));

            _mockConnector.Setup(x => x.Get<TransitionResponse>("api/latest/issue/{issueIdOrKey}/transitions", new KeyValuePair<string, string>("issueIdOrKey", IssueKey), It.IsAny<Dictionary<string, string>>())).Returns(new TransitionResponse
            {
                Transitions = new List<Transition> { _returnTransition }
            }).Verifiable();

        }

        protected void RunIt()
        {
            var jira = new Jira(_mockConnector.Object, null, _mockLogger.Object);

            jira.SetIssueToResolved(IssueKey, new[] { "Done" });
        }


        [TestMethod]
        public void all_mocked_calls_are_called() //I don't think I like this...
        {
            _mockConnector.VerifyAll();
        }
    }

    [TestClass]
    public class Jira_SettingProjectToResoved_Tests : ResolovedSetupContext
    {

        [TestInitialize]
        public override void Context()
        {
            _returnTransition = new Transition { Id = "5", Name = "Done" };
            _mockConnector.Setup(x => x.Post("api/latest/issue/{issueIdOrKey}/transitions", It.IsAny<object>(), HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", IssueKey))).Verifiable();
            base.Context();
            RunIt();
        }

        [TestMethod]
        public void all_mocked_calls_are_called() //I don't think I like this...
        {
            _mockConnector.VerifyAll();
        }

        [TestMethod]
        public void logs_a_message_saying_it_set_the_status()
        {
            _mockLogger.Verify(x => x.Info("Set status on AKey-10 to Done"), Times.Once);
        }
    }
}
