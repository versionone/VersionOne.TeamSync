using System.Collections.Generic;
using log4net;
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    public abstract class worker_bits
    {
        protected Mock<IV1> _mockV1;
        protected Mock<IJiraSettings> _mockJiraSettings; 
        protected Mock<IJira> _mockJira;
        protected Mock<ILog> _mockLogger;
        protected string _projectId = "Scope:1000";
        protected string _jiraKey = "OPC";
        protected string _epicCategory = "EpicCategory:1000";

        protected virtual void BuildContext()
        {
            _mockV1 = new Mock<IV1>();
            _mockJiraSettings = new Mock<IJiraSettings>();
            _mockJiraSettings.Setup(x => x.GetJiraPriorityIdFromMapping(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("3");
            _mockJira = new Mock<IJira>();
            _mockJira.Setup(x => x.JiraSettings).Returns(_mockJiraSettings.Object);
            _mockJira.Setup(x => x.VersionInfo).Returns(new JiraVersionInfo() { VersionNumbers = new[] { "6" } });
            _mockJira.Setup(x => x.V1Project).Returns(_projectId);
            _mockJira.Setup(x => x.JiraProject).Returns(_jiraKey);
            _mockJira.Setup(x => x.EpicCategory).Returns(_epicCategory);
            _mockJira.Setup(x => x.DoneWords).Returns(new[] {"Done"});
            _mockLogger = new Mock<ILog>();
            _mockLogger.Setup(x => x.Logger).Returns(new Mock<ILogger>().Object);
        }
    }

    [TestClass]
    public class Worker_when_there_are_no_new_epics : worker_bits
    {
        private EpicWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>());
            _worker = new EpicWorker(_mockV1.Object, _mockLogger.Object);

            await _worker.CreateEpics(_mockJira.Object);
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
        private EpicWorker _worker;

        public async void DataSetup()
        {
            BuildContext();
            _epic = new Epic() { Number = "5", Description = "descript", Name = "Johnny", ScopeName = "v1" };
            _itemBase = new ItemBase() { Key = _jiraKey };

            _mockV1.Setup(x => x.GetEpicsWithoutReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>()
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, string.Format("Jira {0}", _jiraKey), It.IsAny<string>()));
            _mockJira.Setup(x => x.CreateEpic(_epic, _jiraKey)).Returns(() => _itemBase);

            _epic.Reference.ShouldBeNull();
            _worker = new EpicWorker(_mockV1.Object, _mockLogger.Object);
            await _worker.CreateEpics(_mockJira.Object);
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
            _mockV1.Verify(x => x.CreateLink(_epic, string.Format("Jira {0}", _jiraKey), It.IsAny<string>()), Times.Once);
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
            _mockV1.Verify(x => x.CreateLink(_epic, string.Format("Jira {0}", _jiraKey), It.IsAny<string>()), Times.Once);
        }
    }

    [TestClass]
    public class Worker_when_there_are_no_epics_to_update : worker_bits
    {
        private EpicWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.GetEpicsWithReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>());

            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(new SearchResult());

            _worker = new EpicWorker(_mockV1.Object, _mockLogger.Object);

            await _worker.UpdateEpics(_mockJira.Object);
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
            _mockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), It.IsAny<string>()), Times.Never);
        }
    }

    [TestClass]
    public class Worker_when_there_are_no_updated_epics : worker_bits
    {
        private EpicWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.GetEpicsWithReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>
            {
                new Epic {Name = "Name", Description = "Description", Reference = "key", Priority = "Medium"},
                new Epic {Name = "Name1", Description = "Description", Reference = "key1", Priority = "Medium"},
                new Epic {Name = "Name2", Description = "Description", Reference = "key2", AssetState = "64", Priority = "Medium"},
                new Epic {Name = "Name3", Description = "Description", Reference = "key3", Priority = "Medium"},
                new Epic {Name = "Name4", Description = "Description", Reference = "key4", Priority = "Medium"},
            });

            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(new SearchResult
            {
                issues = new List<Issue>
                {
                    new Issue {Key = "key", Fields = new Fields {Summary = "Name", Description = "Description", Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                    new Issue {Key = "key1", Fields = new Fields {Summary = "Name1", Description = "Description1", Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                    new Issue {Key = "key2", Fields = new Fields {Summary = "Name2", Description = "Description" , Status = new Status {Name = "Done"}, Priority = new Priority{Id = "3"}}},
                    new Issue {Key = "key3", Fields = new Fields {Summary = "Name3", Description = "Description3", Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                    new Issue {Key = "key4", Fields = new Fields {Summary = "Name4", Description = "Description" , Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                }
            });
            _mockJira.SetupGet(x => x.InstanceUrl).Returns("http://jira-6.cloudapp.net:8080");

            _worker = new EpicWorker(_mockV1.Object, _mockLogger.Object);

            await _worker.UpdateEpics(_mockJira.Object);
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
        public void calls_UpdateEpic_twice()
        {
            _mockJira.Verify(x => x.UpdateIssue(It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [TestMethod]
        public void calls_SetEpicTo_ToDo_once()
        {
            _mockJira.Verify(x => x.SetIssueToToDo(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }
    }

    [TestClass]
    public class Worker_when_there_is_1_epic_to_update_matching_one_in_jira : worker_bits
    {
        private Epic _epic;
        private SearchResult _searchResult;
        private EpicWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10", ScopeName = "v1", AssetState = "64" };
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue { Key = "OPC-10", Fields = new Fields() { Status = new Status { Name = "ToDo" } } });

            _mockV1.Setup(x => x.GetEpicsWithReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>
            {
                _epic
            });

            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

            _epic.Reference.ShouldNotBeNull("need a reference");
            _epic.IsClosed().ShouldBeFalse();
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");
            _worker = new EpicWorker(_mockV1.Object, _mockLogger.Object);

            await _worker.UpdateEpics(_mockJira.Object);
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
            _mockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), It.IsAny<string>()), Times.Once());
        }
    }

    [TestClass]
    public class Worker_when_there_is_1_epic_to_update_and_no_match_in_jira : worker_bits
    {
        private Epic _epic;
        private SearchResult _searchResult;
        private EpicWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10" };
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue { Key = "OPC-50" });

            _mockV1.Setup(x => x.GetEpicsWithReference(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>
            {
                _epic
            });

            _mockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

            _epic.Reference.ShouldNotBeNull("need a reference");
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");

            _worker = new EpicWorker(_mockV1.Object, _mockLogger.Object);

            await _worker.UpdateEpics(_mockJira.Object);
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
            _mockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), It.IsAny<string>()), Times.Never);
        }
    }

    [TestClass]
    public class Worker_when_a_VersionOne_epic_is_closed : worker_bits
    {
        private Epic _epic;
        private SearchResult _searchResult;
        private EpicWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic { Reference = "OPC-10", Name = "Johnny", AssetState = "128" };
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue { Key = "OPC-10", Fields = new Fields() { Status = new Status() { Name = "Pending" } } });

            _mockV1.Setup(x => x.GetClosedTrackedEpics(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            _mockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

            _worker = new EpicWorker(_mockV1.Object, _mockLogger.Object);

            await _worker.ClosedV1EpicsSetJiraEpicsToResolved(_mockJira.Object);
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetClosedTrackedEpics(_projectId, _epicCategory), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jira()
        {
            _mockJira.Verify(x => x.GetEpicByKey("OPC-10"), Times.Once());
        }

        [TestMethod]
        public void should_create_a_link_on_v1_epic()
        {
            _mockJira.Verify(x => x.SetIssueToResolved(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }

    }

    [TestClass]
    public class Worker_when_a_VersionOne_epic_is_closed_and_already_updated : worker_bits
    {
        private Epic _epic;
        private SearchResult _searchResult;
        private EpicWorker _epicWorker;
        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic { Reference = "OPC-10", Name = "Johnny", AssetState = "128" };
            _searchResult = new SearchResult();
            _searchResult.issues.Add(new Issue { Key = "OPC-10", Fields = new Fields() { Status = new Status() { Name = "Done" } } });

            _mockV1.Setup(x => x.GetClosedTrackedEpics(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>
            {
                _epic
            });
            _mockV1.Setup(x => x.UpdateEpicReference(_epic));
            _mockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            _mockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

            _epicWorker = new EpicWorker(_mockV1.Object, _mockLogger.Object);
            await _epicWorker.ClosedV1EpicsSetJiraEpicsToResolved(_mockJira.Object);
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            _mockV1.Verify(x => x.GetClosedTrackedEpics(_projectId, _epicCategory), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jira()
        {
            _mockJira.Verify(x => x.GetEpicByKey("OPC-10"), Times.Once());
        }

        [TestMethod]
        public void should_not_set_the_issue_again()
        {
            _mockJira.Verify(x => x.SetIssueToResolved(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        }

    }

    [TestClass]
    public class Worker_when_a_VersionOne_epic_is_deleted : worker_bits
    {
        private Epic _epic;
        private EpicWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _epic = new Epic { Reference = "OPC-10", Number = "E-00001" };

            _mockV1.Setup(x => x.GetDeletedEpics(_projectId, _epicCategory)).ReturnsAsync(new List<Epic>
            {
                _epic
            });
            _mockV1.Setup(x => x.RemoveReferenceOnDeletedEpic(_epic));

            _mockJira.Setup(x => x.DeleteEpicIfExists(_epic.Reference));

            _worker = new EpicWorker(_mockV1.Object, _mockLogger.Object);

            await _worker.DeleteEpics(_mockJira.Object);
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
