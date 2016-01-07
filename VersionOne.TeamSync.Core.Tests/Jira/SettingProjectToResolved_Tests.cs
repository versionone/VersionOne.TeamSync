using System.Collections.Generic;
using System.Net;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.TfsConnector.Interfaces;

namespace VersionOne.TeamSync.Core.Tests.Jira
{
    [TestClass]
    public class Jira_SettingProjectToSomethingThatDoesntExist_Tests : ResolvedSetupContext
    {
        [TestInitialize]
        public override void Context()
        {
            ReturnTransition = new Transition { Id = "5", Name = "Not supported status" };
            base.Context();
            RunIt();
        }

        [TestMethod]
        public void all_mocked_calls_are_called() //I don't think I like this...
        {
            MockConnector.VerifyAll();
        }

        [TestMethod]
        public void does_not_log_a_message_saying_it_set_the_status()
        {
            MockLogger.Verify(x => x.Info("Set status on AKey-10 to Done"), Times.Never);
        }

        [TestMethod]
        public void gives_an_error_message_that_says_it_failed_moving_to_unsupported_statuses()
        {
            MockLogger.Verify(x => x.Error("None or multiple transistions exists for AKey-10 with the status of Done.  This epic will not be updated"), Times.Once);
        }
    }

    public abstract class ResolvedSetupContext
    {
        protected const string IssueKey = "AKey-10";

        protected Mock<IJiraConnector> MockConnector = new Mock<IJiraConnector>();
        protected Mock<IV1Log> MockLogger = new Mock<IV1Log>();
        protected Transition ReturnTransition = new Transition();

        public virtual void Context()
        {
            MockLogger.Setup(x => x.Error(It.IsAny<string>()));

            MockConnector.Setup(x => x.Get<TransitionResponse>("api/latest/issue/{issueIdOrKey}/transitions", new KeyValuePair<string, string>("issueIdOrKey", IssueKey), It.IsAny<Dictionary<string, string>>())).Returns(new TransitionResponse
            {
                Transitions = new List<Transition> { ReturnTransition }
            }).Verifiable();
        }

        protected void RunIt()
        {
            var mockLoggerFactory = new Mock<IV1LogFactory>();
            mockLoggerFactory.Setup(x => x.Create<JiraWorker.Domain.Jira>()).Returns(MockLogger.Object);

            var jira = new JiraWorker.Domain.Jira(MockConnector.Object, mockLoggerFactory.Object, null);

            jira.SetIssueToResolved(IssueKey, new[] { "Done" });
        }
    }

    [TestClass]
    public class Jira_SettingProjectToResolved_Tests : ResolvedSetupContext
    {
        [TestInitialize]
        public override void Context()
        {
            ReturnTransition = new Transition { Id = "5", Name = "Done" };
            MockConnector.Setup(x => x.Post("api/latest/issue/{issueIdOrKey}/transitions", It.IsAny<object>(), HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", IssueKey))).Verifiable();
            base.Context();
            RunIt();
        }

        [TestMethod]
        public void all_mocked_calls_are_called() //I don't think I like this...
        {
            MockConnector.VerifyAll();
        }

        [TestMethod]
        public void logs_a_message_saying_it_set_the_status()
        {
            MockLogger.Verify(x => x.Info("Set status on AKey-10 to Done"), Times.Once);
        }
    }
}
