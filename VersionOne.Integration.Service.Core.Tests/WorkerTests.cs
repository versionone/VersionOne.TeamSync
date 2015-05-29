using System.Collections.Generic;
using System.Security.Policy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.JiraConnector.Entities;

namespace VersionOne.Integration.Service.Worker.Tests
{
    public abstract class worker_bits
    {
        protected VersionOneToJiraWorker _worker;
        protected Mock<IV1> _mockV1;
        protected Mock<IJira> _mockJira;
        protected string _projectId = "Scope:1000";
        protected string _jiraKey = "OPC";
        protected string _epicCategory = "EpicCategory:1000";

        protected void BuildContext()
        {
            _mockV1 = new Mock<IV1>();
            _mockJira = new Mock<IJira>();
            _worker = new VersionOneToJiraWorker(_mockV1.Object);
        }

        protected V1JiraInfo MakeInfo()
        {
            return new V1JiraInfo(_projectId, _jiraKey, _epicCategory, _mockJira.Object);
        }
    }

    [TestClass]
    public class Worker_when_there_are_no_new_epics : worker_bits
    {
        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>());
            var jiraInfo = MakeInfo();
            await _worker.CreateEpics(jiraInfo);
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

    public class Worker_when_there_is_a_new_epic_in_v1 : worker_bits
    {
        protected Epic _epic;
        protected ItemBase _itemBase;

        public async void DataSetup()
        {
            BuildContext();
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", ProjectName = "v1" };
            _itemBase = new ItemBase() { Key = _jiraKey };

            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));
            _mockJira.Setup(x => x.CreateEpic(_epic, _jiraKey)).Returns(() => _itemBase);

            _epic.Reference.ShouldBeNull();
            var jiraInfo = MakeInfo();
            
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
            _mockV1.Verify(x => x.GetEpicsWithoutReference(_projectId, _epicCategory), Times.Once);
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
            _mockV1.Verify(x => x.GetEpicsWithoutReference(_projectId, _epicCategory), Times.Once);
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
    public class Worker_when_there_are_no_epics_to_update : worker_bits
    {
        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _mockV1 = new Mock<IV1>();
            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>());

            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(new SearchResult());

			_worker = new VersionOneToJiraWorker(_mockV1.Object);
            var jiraInfo = MakeInfo();

            await _worker.UpdateEpics(jiraInfo);
        }

        [TestMethod]
        public void calls_GetEpicWithReference_once()
        {
            _mockV1.Verify(x => x.GetEpicsWithReference(_projectId, _epicCategory), Times.Once);
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
    public class Worker_when_there_is_1_epic_to_update_matching_one_in_jira : worker_bits
    {
        private Epic _epic;
        private SearchResult _searchResult;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10", ProjectName = "v1"};
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue(){Key = "OPC-10"});

            _mockV1.Setup(x => x.GetEpicsWithReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });

			_mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

            _epic.Reference.ShouldNotBeNull("need a reference");
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");
            var jiraInfo = MakeInfo();

            await _worker.UpdateEpics(jiraInfo);
        }

        [TestMethod]
        public void should_call_EpicsWithReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithReference(_projectId, _epicCategory), Times.Once);
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
    public class Worker_when_there_is_1_epic_to_update_and_no_match_in_jira : worker_bits
    {
        private Epic _epic;
        private SearchResult _searchResult;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10" };
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue() { Key = "OPC-50" });

            _mockV1.Setup(x => x.GetEpicsWithReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });

            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

            _epic.Reference.ShouldNotBeNull("need a reference");
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");

            var jiraInfo = MakeInfo();

            await _worker.UpdateEpics(jiraInfo);
        }

        [TestMethod]
        public void should_call_EpicsWithReference_one_time()
        {
            _mockV1.Verify(x => x.GetEpicsWithReference(_projectId, _epicCategory), Times.Once);
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
    public class Worker_when_a_VersionOne_epic_is_closed : worker_bits
    {
        private Epic _epic;
        private SearchResult _searchResult;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic() { Reference = "OPC-10", Name = "Johnny", AssetState = "128"};
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue() { Key = "OPC-10" });

            _mockV1.Setup(x => x.GetClosedTrackedEpics(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            _mockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

            var jiraInfo = MakeInfo();

            await _worker.ClosedV1EpicsSetJiraEpicsToResolved(jiraInfo);
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetClosedTrackedEpics(_projectId, _epicCategory), Times.Once);
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
    public class Worker_when_a_VersionOne_epic_is_deleted : worker_bits
    {
        private Epic _epic;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic() { Reference = "OPC-10", Number = "E-00001"};

            _mockV1.Setup(x => x.GetDeletedEpics(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.RemoveReferenceOnDeletedEpic(_epic));

            _mockJira.Setup(x => x.DeleteEpicIfExists(_epic.Reference));

            _worker = new VersionOneToJiraWorker(_mockV1.Object);
            var jiraInfo = MakeInfo();

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
