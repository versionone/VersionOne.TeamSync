using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.Worker;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests.Workers
{
    [TestClass]
    public class Worker_when_there_are_no_new_epics : worker_bits
    {
        private EpicWorker _epicWorker;
        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>());
            var jiraInfo = MakeInfo();

            _epicWorker = new EpicWorker(_mockV1.Object, _mockLogger.Object);

            await _epicWorker.CreateEpics(jiraInfo);
        }

        [TestMethod]
        public void calls_the_GetEpicsWithoutReference_once()
        {
            _mockV1.Verify(x => x.GetEpicsWithoutReference(_projectId, _epicCategory), Times.Once);
        }

        [TestMethod]
        public void do_not_call_the_jira_api()
        {
            _mockJira.Verify(x => x.CreateEpic(It.IsAny<Epic>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void does_not_try_to_create_a_link_on_v1_epic()
        {
            _mockV1.Verify(x => x.CreateLink(It.IsAny<Epic>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

    }
}
