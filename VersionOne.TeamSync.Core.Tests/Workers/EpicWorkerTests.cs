using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.JiraWorker;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.VersionOne.Domain;

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
            //MockV1.Setup(x => x.GetEpicsWithoutReference(ProjectId, EpicCategory)).ReturnsAsync(new List<Epic>());
            MockV1.Setup(x => x.GetEpicsWithoutReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>());

            _epicWorker = new EpicWorker(MockV1.Object, MockV1Log.Object);

            await _epicWorker.CreateEpics(MockJira.Object);
        }

        [TestMethod]
        public void calls_the_GetEpicsWithoutReference_once()
        {
            MockV1.Verify(x => x.GetEpicsWithoutReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void do_not_call_the_jira_api()
        {
            MockJira.Verify(x => x.CreateEpic(It.IsAny<Epic>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void does_not_try_to_create_a_link_on_v1_epic()
        {
            MockV1.Verify(x => x.CreateLink(It.IsAny<Epic>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
