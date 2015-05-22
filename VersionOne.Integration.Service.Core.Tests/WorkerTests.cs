using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.SDK.Jira.Entities;

namespace VersionOne.Integration.Service.Worker.Tests
{
    [TestClass]
    public class Worker_when_there_are_no_new_epics
    {
        private VersionOneToJiraWorker _worker;
        private Mock<IV1> _mockV1;
        private Mock<IJira> _mockJira;

        [TestInitialize]
        public async void Context()
        {
            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithoutReference()).ReturnsAsync(new List<Epic>());
            _mockJira = new Mock<IJira>();

            var classUnderTest = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object);
            await classUnderTest.CreateEpics();
        }

        [TestMethod]
        public void calls_the_GetEpicsWithoutReference_once()
        {
            _mockV1.Verify(x => x.GetEpicsWithoutReference(), Times.Once);
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

    [TestClass]
    public class Worker_when_there_is_1_new_epic
    {
        private VersionOneToJiraWorker _worker;
        private Mock<IV1> _mockV1;
        private Epic _epic;
        private ItemBase _itemBase;
        private Mock<IJira> _mockJira;

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny" };
            _itemBase = new ItemBase() { Key = "OPC-10" };

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithoutReference()).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.CreateEpic(_epic, It.IsAny<string>())).Returns(() => _itemBase);

            _worker = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object);
            
            _epic.Reference.ShouldBeNull();
            
            await _worker.CreateEpics();
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithoutReference(), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jir()
        {
            _mockJira.Verify(x => x.CreateEpic(_epic, "OPC"), Times.Once());
        }

        [TestMethod]
        public void should_pass_along_the_key_to_epic_reference()
        {
            _epic.Reference.ShouldEqual(_itemBase.Key);
        }

        [TestMethod]
        public void should_create_a_link_on_v1_epic()
        {
            _mockV1.Verify(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()), Times.Once);
        }

    }
}
