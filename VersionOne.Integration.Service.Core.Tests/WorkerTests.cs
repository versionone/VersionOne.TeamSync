using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.JiraConnector.Entities;

namespace VersionOne.Integration.Service.Worker.Tests
{
    [TestClass]
    public class Worker_when_there_are_no_new_epics
    {
        private VersionOneToJiraWorker _worker;
        private Mock<IV1> _mockV1;
        private Mock<IJira> _mockJira;
        private string _projectId = "1000";
        private string _jiraKey = "OPC";
        [TestInitialize]
        public async void Context()
        {
            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId)).ReturnsAsync(new List<Epic>());
            _mockJira = new Mock<IJira>();

            var jiraInfo = new V1JiraInfo(_projectId, _jiraKey, _mockJira.Object);

            var classUnderTest = new VersionOneToJiraWorker(_mockV1.Object);
            await classUnderTest.CreateEpics(jiraInfo);
        }

        [TestMethod]
        public void calls_the_GetEpicsWithoutReference_once()
        {
            _mockV1.Verify(x => x.GetEpicsWithoutReference(_projectId), Times.Once);
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

    public class Worker_when_there_is_a_new_epic_in_v1
    {
        protected VersionOneToJiraWorker _worker;
        protected Mock<IV1> _mockV1;
        protected Epic _epic;
        protected ItemBase _itemBase;
        protected Mock<IJira> _mockJira;
        protected Dictionary<string, string> _mappingValues;
        protected string _projectId = "1000";
        protected string _jiraKey = "OPC";

        public async void DataSetup()
        {
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", ProjectName = "v1" };
            _itemBase = new ItemBase() { Key = _jiraKey };

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.CreateEpic(_epic, _jiraKey)).Returns(() => _itemBase);

            _epic.Reference.ShouldBeNull();
            var jiraInfo = new V1JiraInfo(_projectId, _jiraKey, _mockJira.Object);

            _worker = new VersionOneToJiraWorker(_mockV1.Object);
            await _worker.CreateEpics(jiraInfo);
        }
    }

    [TestClass]
    public class and_it_is_a_mapped_project : Worker_when_there_is_a_new_epic_in_v1
    {
        [TestInitialize]
        public void Context()
        {
            DataSetup();
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithoutReference(_projectId), Times.Once);
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
    public class and_the_jira_project_contains_a_reserved_word : Worker_when_there_is_a_new_epic_in_v1
    {
        [TestInitialize]
        public void Context()
        {
            _jiraKey = "AS";
            DataSetup();
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithoutReference(_projectId), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jira_without_modifying_reserved_word()
        {
            _mockJira.Verify(x => x.CreateEpic(_epic, "AS"), Times.Once());
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
        protected string _projectId = "1000";
        protected string _jiraKey = "OPC";

        [TestInitialize]
        public async void Context()
        {
            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId)).ReturnsAsync(new List<Epic>());

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(new SearchResult());

			_worker = new VersionOneToJiraWorker(_mockV1.Object);
            var jiraInfo = new V1JiraInfo(_projectId, _jiraKey, _mockJira.Object);

            await _worker.UpdateEpics(jiraInfo);
        }

        [TestMethod]
        public void calls_GetEpicWithReference_once()
        {
            _mockV1.Verify(x =>x.GetEpicsWithReference(_projectId), Times.Once);
        }

        [TestMethod]
        public void calls_GetEpicsInProject_once()
        {
            _mockJira.Verify(x => x.GetEpicsInProject(_jiraKey), Times.Once);
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
        protected string _projectId = "1000";
        protected string _jiraKey = "OPC";

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10", ProjectName = "v1"};
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue(){Key = "OPC-10"});

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithReference(_projectId)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });

            _mockJira = new Mock<IJira>();
			_mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

            _worker = new VersionOneToJiraWorker(_mockV1.Object);
            _epic.Reference.ShouldNotBeNull("need a reference");
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");
            var jiraInfo = new V1JiraInfo(_projectId, _jiraKey, _mockJira.Object);

            await _worker.UpdateEpics(jiraInfo);
        }

        [TestMethod]
        public void should_call_EpicsWithReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithReference(_projectId), Times.Once);
        }

        [TestMethod]
        public void should_call_GetEpicsInProject_jira()
        {
            _mockJira.Verify(x => x.GetEpicsInProject(It.IsAny<string>()), Times.Once());
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
        protected string _projectId = "1000";
        protected string _jiraKey = "OPC";

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10" };
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue() { Key = "OPC-50" });

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithReference(_projectId)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

			_worker = new VersionOneToJiraWorker(_mockV1.Object);
            _epic.Reference.ShouldNotBeNull("need a reference");
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");

            var jiraInfo = new V1JiraInfo(_projectId, _jiraKey, _mockJira.Object);

            await _worker.UpdateEpics(jiraInfo);
        }

        [TestMethod]
        public void should_call_EpicsWithReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithReference(_projectId), Times.Once);
        }

        [TestMethod]
        public void should_call_GetEpicsInProject_jira()
        {
            _mockJira.Verify(x => x.GetEpicsInProject(_jiraKey), Times.Once());
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
        protected string _projectId = "1000";
        protected string _jiraKey = "OPC";

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Reference = "OPC-10", Name = "Johnny", AssetState = "128"};
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue() { Key = "OPC-10" });

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetClosedTrackedEpics(_projectId)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

            _worker = new VersionOneToJiraWorker(_mockV1.Object);
            var jiraInfo = new V1JiraInfo(_projectId, _jiraKey, _mockJira.Object);

            await _worker.ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetClosedTrackedEpics(_projectId), Times.Once);
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
        protected string _projectId = "1000";
        protected string _jiraKey = "OPC";

        [TestInitialize]
        public async void Context()
        {
            _epic = new Epic() { Reference = "OPC-10", Number = "E-00001"};

            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetDeletedEpics(_projectId)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.RemoveReferenceOnDeletedEpic(_epic));

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.DeleteEpicIfExists(_epic.Reference));

            _worker = new VersionOneToJiraWorker(_mockV1.Object);
            var jiraInfo = new V1JiraInfo(_projectId, _jiraKey, _mockJira.Object);

            await _worker.DeleteEpics(jiraInfo);
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
