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

            var classUnderTest = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object, new Dictionary<string, string>(){{"v1", "OPC"}});
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
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", ProjectName = "v1"};
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

            _worker = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object, new Dictionary<string, string>()
            {
	            {"v1", "OPC"}
            });
            
            _epic.Reference.ShouldBeNull();
            
            await _worker.CreateEpics();
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithoutReference(), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jira()
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

    [TestClass]
    public class Worker_when_there_are_no_epics_to_update
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
            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(new SearchResult());

			_worker = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object, new Dictionary<string, string>() { { "v1", "OPC" } });

            await _worker.UpdateEpics();
        }

        [TestMethod]
        public void calls_GetEpicWithReference_once()
        {
            _mockV1.Verify(x =>x.GetEpicsWithReference(), Times.Once);
        }

        [TestMethod]
        public void calls_GetEpicsInProject_once()
        {
            _mockJira.Verify(x => x.GetEpicsInProjects(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [TestMethod]
        public void never_calls_UpdateEpic()
        {
            _mockJira.Verify(x => x.UpdateEpic(It.IsAny<Epic>(), It.IsAny<string>()),Times.Never);
        }
    }

    [TestClass]
    public class Worker_when_there_is_1_epic_to_update_matching_one_in_jira
    {
        private VersionOneToJiraWorker _worker;
        private Mock<IV1> _mockV1;
        private Epic _epic;
        private SearchResult _searchResult;
        private Mock<IJira> _mockJira;

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10", ProjectName = "v1"};
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue(){Key = "OPC-10"});

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithReference()).ReturnsAsync(new List<Epic>()
            {
                _epic
            });

            _mockJira = new Mock<IJira>();
			_mockJira.Setup(x => x.GetEpicsInProjects(It.IsAny<IEnumerable<string>>())).Returns(_searchResult);

			_worker = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object, new Dictionary<string, string>() { { "v1", "OPC" } });
            _epic.Reference.ShouldNotBeNull("need a reference");
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");

            await _worker.UpdateEpics();
        }

        [TestMethod]
        public void should_call_EpicsWithReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithReference(), Times.Once);
        }

        [TestMethod]
        public void should_call_GetEpicsInProject_jira()
        {
            _mockJira.Verify(x => x.GetEpicsInProjects(It.IsAny<IEnumerable<string>>()), Times.Once());
        }

        [TestMethod]
        public void should_call_UpdateEpic_jira()
        {
            _mockJira.Verify(x => x.UpdateEpic(It.IsAny<Epic>(), It.IsAny<string>()), Times.Once());
        }
    }

    [TestClass]
    public class Worker_when_there_is_1_epic_to_update_and_no_match_in_jira
    {
        private VersionOneToJiraWorker _worker;
        private Mock<IV1> _mockV1;
        private Epic _epic;
        private SearchResult _searchResult;
        private Mock<IJira> _mockJira;

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10" };
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue() { Key = "OPC-50" });

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithReference()).ReturnsAsync(new List<Epic>()
            {
                _epic
            });

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

			_worker = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object, new Dictionary<string, string>() { { "v1", "OPC" } });
            _epic.Reference.ShouldNotBeNull("need a reference");
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");

            await _worker.UpdateEpics();
        }

        [TestMethod]
        public void should_call_EpicsWithReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithReference(), Times.Once);
        }

        [TestMethod]
        public void should_call_GetEpicsInProject_jira()
        {
            _mockJira.Verify(x => x.GetEpicsInProjects(It.IsAny<IEnumerable<string>>()), Times.Once());
        }

        [TestMethod]
        public void does_not_update_the_epic_in_jira()
        {
            _mockJira.Verify(x => x.UpdateEpic(It.IsAny<Epic>(), It.IsAny<string>()), Times.Never);
        }
    }

    [TestClass]
    public class Worker_when_a_VersionOne_epic_is_closed
    {
        private VersionOneToJiraWorker _worker;
        private Mock<IV1> _mockV1;
        private Epic _epic;
        private SearchResult _searchResult;
        private Mock<IJira> _mockJira;

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Reference = "OPC-10", Name = "Johnny", AssetState = "128"};
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue() { Key = "OPC-10" });

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetClosedTrackedEpics()).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

			_worker = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object, new Dictionary<string, string>() { { "v1", "OPC" } });

            await _worker.ClosedV1EpicsSetJiraEpicsToResolved();
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetClosedTrackedEpics(), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jir()
        {
            _mockJira.Verify(x => x.GetEpicByKey("OPC-10"), Times.Once());
        }

        [TestMethod]
        public void should_create_a_link_on_v1_epic()
        {
            _mockJira.Verify(x => x.SetIssueToResolved(It.IsAny<string>()), Times.Once);
        }

    }

    [TestClass]
    public class Worker_when_a_VersionOne_epic_is_deleted
    {
        private VersionOneToJiraWorker _worker;
        private Mock<IV1> _mockV1;
        private Epic _epic;
        private Mock<IJira> _mockJira;

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Reference = "OPC-10", Number = "E-00001"};

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetDeletedEpics()).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.RemoveReferenceOnDeletedEpic(_epic));

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.DeleteEpicIfExists(_epic.Reference));

			_worker = new VersionOneToJiraWorker(_mockV1.Object, _mockJira.Object, new Dictionary<string, string>() { { "v1", "OPC" } });

            await _worker.DeleteEpics();
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.RemoveReferenceOnDeletedEpic(_epic), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jir()
        {
            _mockJira.Verify(x => x.DeleteEpicIfExists("OPC-10"), Times.Once());
        }

    }

}
