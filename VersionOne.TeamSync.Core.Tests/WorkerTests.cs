using System;
using System.Collections.Generic;
using log4net;
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraWorker;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.VersionOne.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    public abstract class worker_bits
    {
        protected Mock<IV1> MockV1;
        protected Mock<IJiraSettings> MockJiraSettings;
        protected Mock<IJira> MockJira;
        protected Mock<ILog> MockLog;
        protected Mock<V1Log> MockV1Log;
        protected string ProjectId = "Scope:1000";
        protected string JiraKey = "OPC";
        protected string EpicCategory = "EpicCategory:1000";
        protected string InstanceUrl = "http://localhost:8080";
        protected User Assignee;

        protected virtual void BuildContext()
        {
            MockV1 = new Mock<IV1>();

            MockJiraSettings = new Mock<IJiraSettings>();
            MockJiraSettings.Setup(x => x.GetJiraPriorityIdFromMapping(It.IsAny<string>(), "Medium")).Returns("3");
            MockJiraSettings.Setup(x => x.GetV1PriorityIdFromMapping(It.IsAny<string>(), "Medium"))
                .Returns("WorkitemPriority:139");
            MockJiraSettings.Setup(
                x => x.GetJiraStatusFromMapping(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("InProgress");

            JiraSettings.Instance = MockJiraSettings.Object;

            MockJira = new Mock<IJira>();
            MockJira.SetupGet(x => x.InstanceUrl).Returns(InstanceUrl);
            MockJira.Setup(x => x.JiraSettings).Returns(MockJiraSettings.Object);
            MockJira.Setup(x => x.VersionInfo).Returns(new JiraVersionInfo() { VersionNumbers = new[] { "6" } });
            MockJira.Setup(x => x.V1Project).Returns(ProjectId);
            MockJira.Setup(x => x.JiraProject).Returns(JiraKey);
            MockJira.Setup(x => x.EpicCategory).Returns(EpicCategory);
            MockJira.Setup(x => x.DoneWords).Returns(new[] { "Done" });

            MockLog = new Mock<ILog>();
            MockLog.Setup(x => x.Logger).Returns(new Mock<ILogger>().Object);
            MockV1Log = new Mock<V1Log>(MockLog.Object);

            Assignee = new User
            {
                displayName = "Administrator",
                name = "admin",
                emailAddress = "admin@versionone.com"
            };
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
            MockV1.Setup(x => x.GetEpicsWithoutReference(ProjectId, EpicCategory)).ReturnsAsync(new List<Epic>());
            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

            await _worker.CreateEpics(MockJira.Object);
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

    public class Worker_when_there_is_a_new_epic_in_v1 : worker_bits
    {
        protected Epic Epic = new Epic { Number = "5", Description = "descript", Name = "Johnny", ScopeName = "v1", Status = "", CreateDateUTC = "01-01-2015", ChangeDateUTC = "01-03-2015"};
        protected ItemBase ItemBase;

        private EpicWorker _worker;

        public async void DataSetup()
        {
            BuildContext();
            ItemBase = new ItemBase { Key = JiraKey };

            MockV1.Setup(x => x.GetEpicsWithoutReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                Epic
            });
            MockJira.Setup(x => x.GetIssueTransitionId(It.IsAny<string>(), It.IsAny<string>())).Returns("3");
            MockV1.Setup(x => x.UpdateEpicReference(Epic));
            MockV1.Setup(x => x.CreateLink(Epic, string.Format("Jira {0}", JiraKey), It.IsAny<string>()));
            MockJira.Setup(x => x.CreateEpic(Epic, JiraKey)).Returns(() => ItemBase);

            Epic.Reference.ShouldBeNull();
            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);
            await _worker.CreateEpics(MockJira.Object);
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
            MockV1.Verify(x => x.GetEpicsWithoutReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jira()
        {
            MockJira.Verify(x => x.CreateEpic(Epic, "OPC"), Times.Once());
        }

        [TestMethod]
        public void should_pass_along_the_key_to_epic_reference()
        {
            Epic.Reference.ShouldEqual(ItemBase.Key);
        }

        [TestMethod]
        public void should_create_a_link_on_v1_epic()
        {
            MockV1.Verify(x => x.CreateLink(Epic, string.Format("Jira {0}", JiraKey), It.IsAny<string>()), Times.Once);
        }
    }

    [TestClass]
    public class and_the_jira_project_contains_a_reserved_word : Worker_when_there_is_a_new_epic_in_v1
    {
        [TestInitialize]
        public void Context()
        {
            JiraKey = "AS";
            DataSetup();
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            MockV1.Verify(x => x.GetEpicsWithoutReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jira_without_modifying_reserved_word()
        {
            MockJira.Verify(x => x.CreateEpic(Epic, "AS"), Times.Once());
        }

        [TestMethod]
        public void should_pass_along_the_key_to_epic_reference()
        {
            Epic.Reference.ShouldEqual(ItemBase.Key);
        }

        [TestMethod]
        public void should_create_a_link_on_v1_epic()
        {
            MockV1.Verify(x => x.CreateLink(Epic, string.Format("Jira {0}", JiraKey), It.IsAny<string>()), Times.Once);
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
            MockV1.Setup(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>());

            MockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(new SearchResult());

            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

            await _worker.UpdateEpics(MockJira.Object);
        }

        [TestMethod]
        public void calls_GetEpicWithReference_once()
        {
            MockV1.Verify(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void calls_GetEpicsInProject_once()
        {
            MockJira.Verify(x => x.GetEpicsInProject(JiraKey), Times.Once);
        }

        [TestMethod]
        public void never_calls_UpdateEpic()
        {
            MockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), It.IsAny<string>()), Times.Never);
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
            MockV1.Setup(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                new Epic {Name = "Name", Description = "Description", Reference = "key", Priority = "Medium"},
                new Epic {Name = "Name1", Description = "Description", Reference = "key1", Priority = "Medium"},
                new Epic {Name = "Name2", Description = "Description", Reference = "key2", AssetState = "64", Priority = "Medium"},
                new Epic {Name = "Name3", Description = "Description", Reference = "key3", Priority = "Medium"},
                new Epic {Name = "Name4", Description = "Description", Reference = "key4", Priority = "Medium"},
            });

            MockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(new SearchResult
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
            MockJira.SetupGet(x => x.InstanceUrl).Returns("http://jira-6.cloudapp.net:8080");

            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

            await _worker.UpdateEpics(MockJira.Object);
        }

        [TestMethod]
        public void calls_GetEpicWithReference_once()
        {
            MockV1.Verify(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void calls_GetEpicsInProject_once()
        {
            MockJira.Verify(x => x.GetEpicsInProject(JiraKey), Times.Once);
        }

        [TestMethod]
        public void calls_UpdateEpic_twice()
        {
            MockJira.Verify(x => x.UpdateIssue(It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
        }

        //[TestMethod]
        //public void calls_SetEpicTo_ToDo_once()
        //{
        //    MockJira.Verify(x => x.SetIssueToToDo(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        //}
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
            _searchResult.issues.Add(new Issue
            {
                Key = "OPC-10",
                Fields = new Fields
                    {
                        Status = new Status { Name = "ToDo" },
                        Priority = new Priority
                        {
                            Id = "3",
                            Name = "Medium"
                        }
                    }
            });

            MockV1.Setup(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                _epic
            });

            MockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

            _epic.Reference.ShouldNotBeNull("need a reference");
            _epic.IsClosed().ShouldBeFalse();
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");
            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

            await _worker.UpdateEpics(MockJira.Object);
        }

        [TestMethod]
        public void should_call_EpicsWithReference_one_time()
        {
            MockV1.Verify(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void should_call_GetEpicsInProject_jira()
        {
            MockJira.Verify(x => x.GetEpicsInProject(It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void should_call_UpdateEpic_jira()
        {
            MockJira.Verify(x => x.UpdateIssue(It.IsAny<object>(), It.IsAny<string>()), Times.Once());
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

            MockV1.Setup(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                _epic
            });

            MockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(_searchResult);

            _epic.Reference.ShouldNotBeNull("need a reference");
            _searchResult.issues[0].Key.ShouldNotBeNull("need a reference");

            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

            await _worker.UpdateEpics(MockJira.Object);
        }

        [TestMethod]
        public void should_call_EpicsWithReference_one_time()
        {
            MockV1.Verify(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void should_call_GetEpicsInProject_jira()
        {
            MockJira.Verify(x => x.GetEpicsInProject(JiraKey), Times.Once());
        }

        [TestMethod]
        public void does_not_update_the_epic_in_jira()
        {
            MockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), It.IsAny<string>()), Times.Never);
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

            MockV1.Setup(x => x.GetClosedTrackedEpicsUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                _epic
            });
            MockV1.Setup(x => x.UpdateEpicReference(_epic));
            MockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            MockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

            await _worker.ClosedV1EpicsSetJiraEpicsToResolved(MockJira.Object);
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            MockV1.Verify(x => x.GetClosedTrackedEpicsUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jira()
        {
            MockJira.Verify(x => x.GetEpicByKey("OPC-10"), Times.Once());
        }

        [TestMethod]
        public void should_create_a_link_on_v1_epic()
        {
            MockJira.Verify(x => x.SetIssueToResolved(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
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

            MockV1.Setup(x => x.GetClosedTrackedEpicsUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                _epic
            });
            MockV1.Setup(x => x.UpdateEpicReference(_epic));
            MockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

            MockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

            _epicWorker = new EpicWorker(MockV1.Object, MockV1Log.Object);
            await _epicWorker.ClosedV1EpicsSetJiraEpicsToResolved(MockJira.Object);
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            MockV1.Verify(x => x.GetClosedTrackedEpicsUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jira()
        {
            MockJira.Verify(x => x.GetEpicByKey("OPC-10"), Times.Once());
        }

        [TestMethod]
        public void should_not_set_the_issue_again()
        {
            MockJira.Verify(x => x.SetIssueToResolved(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        }
    }

    //[TestClass]
    //public class Worker_when_a_VersionOne_epic_is_closed : worker_bits
    //{
    //    private Epic _epic;
    //    private SearchResult _searchResult;
    //    private EpicWorker _worker;

    //    [TestInitialize]
    //    public async void Context()
    //    {
    //        BuildContext();
    //        _epic = new Epic { Reference = "OPC-10", Name = "Johnny", AssetState = "128" };
    //        _searchResult = new SearchResult();
    //        _searchResult.issues.Add(new Issue { Key = "OPC-10", Fields = new Fields() { Status = new Status() { Name = "Pending" } } });

    //        MockV1.Setup(x => x.GetClosedTrackedEpics(ProjectId, EpicCategory)).ReturnsAsync(new List<Epic>
    //        {
    //            _epic
    //        });
    //        MockV1.Setup(x => x.UpdateEpicReference(_epic));
    //        MockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

    //        MockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

    //        _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

    //        await _worker.ClosedV1EpicsSetJiraEpicsToClosed(MockJira.Object);
    //    }

    //    [TestMethod]
    //    public void should_call_EpicsWithoutReference_one_time()
    //    {
    //        MockV1.Verify(x => x.GetClosedTrackedEpics(ProjectId, EpicCategory), Times.Once);
    //    }

    //    [TestMethod]
    //    public void should_call_CreateEpic_on_jira()
    //    {
    //        MockJira.Verify(x => x.GetEpicByKey("OPC-10"), Times.Once());
    //    }

    //    [TestMethod]
    //    public void should_create_a_link_on_v1_epic()
    //    {
    //        MockJira.Verify(x => x.SetIssueToResolved(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
    //    }
    //}

    //[TestClass]
    //public class Worker_when_a_VersionOne_epic_is_closed_and_already_updated : worker_bits
    //{
    //    private Epic _epic;
    //    private SearchResult _searchResult;
    //    private EpicWorker _epicWorker;

    //    [TestInitialize]
    //    public async void Context()
    //    {
    //        BuildContext();
    //        _epic = new Epic { Reference = "OPC-10", Name = "Johnny", AssetState = "128" };
    //        _searchResult = new SearchResult();
    //        _searchResult.issues.Add(new Issue { Key = "OPC-10", Fields = new Fields() { Status = new Status() { Name = "Done" } } });

    //        MockV1.Setup(x => x.GetClosedTrackedEpics(ProjectId, EpicCategory)).ReturnsAsync(new List<Epic>
    //        {
    //            _epic
    //        });
    //        MockV1.Setup(x => x.UpdateEpicReference(_epic));
    //        MockV1.Setup(x => x.CreateLink(_epic, "Jira Epic", It.IsAny<string>()));

    //        MockJira.Setup(x => x.GetEpicByKey(It.IsAny<string>())).Returns(() => _searchResult);

    //        _epicWorker = new EpicWorker(MockV1.Object, MockV1Log.Object);
    //        await _epicWorker.ClosedV1EpicsSetJiraEpicsToClosed(MockJira.Object);
    //    }

    //    [TestMethod]
    //    public void should_call_EpicsWithoutReference_one_time()
    //    {
    //        MockV1.Verify(x => x.GetClosedTrackedEpics(ProjectId, EpicCategory), Times.Once);
    //    }

    //    [TestMethod]
    //    public void should_call_CreateEpic_on_jira()
    //    {
    //        MockJira.Verify(x => x.GetEpicByKey("OPC-10"), Times.Once());
    //    }

    //    [TestMethod]
    //    public void should_not_set_the_issue_again()
    //    {
    //        MockJira.Verify(x => x.SetIssueToResolved(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
    //    }

    //}

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

            MockV1.Setup(x => x.GetDeletedEpicsUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                _epic
            });
            MockV1.Setup(x => x.RemoveReferenceOnDeletedEpic(_epic));

            MockJira.Setup(x => x.DeleteEpicIfExists(_epic.Reference));

            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

            await _worker.DeleteEpics(MockJira.Object);
        }

        [TestMethod]
        public void should_call_EpicsWithoutReference_one_time()
        {
            MockV1.Verify(x => x.RemoveReferenceOnDeletedEpic(_epic), Times.Once);
        }

        [TestMethod]
        public void should_call_CreateEpic_on_jir()
        {
            MockJira.Verify(x => x.DeleteEpicIfExists("OPC-10"), Times.Once());
        }
    }
}