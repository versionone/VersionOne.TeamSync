using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{

    [TestClass]
    public class and_it_has_no_status_set : Worker_when_there_is_a_new_epic_in_v1
    {
        [TestInitialize]
        public void Context()
        {
            DataSetup();
        }

        [TestMethod]
        public void do_not_call_GetIssueTransitionId()
        {
            MockJira.Verify(x => x.GetIssueTransitionId(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void do_not_call_RunTransitionOnIssue()
        {
            MockJira.Verify(x => x.RunTransitionOnIssue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    [TestClass]
    public class and_it_has_status_set : Worker_when_there_is_a_new_epic_in_v1
    {
        [TestInitialize]
        public void Context()
        {
            Epic.Status = "Test";
            DataSetup();
        }

        [TestMethod]
        public void call_GetIssueTransitionId_once()
        {
            MockJira.Verify(x => x.GetIssueTransitionId(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void call_RunTransitionOnIssue_once()
        {
            MockJira.Verify(x => x.RunTransitionOnIssue("3", It.IsAny<string>()), Times.Once);
        }
    }

    [TestClass]
    public class Worker_when_there_are_epics_to_transition : worker_bits
    {
        private EpicWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            MockV1.Setup(x => x.GetEpicsWithReference(ProjectId, EpicCategory)).ReturnsAsync(new List<Epic>
            {
                new Epic {Name = "Name", Description = "Description", Reference = "key", Priority = "Medium", Status = "Done"},
                new Epic {Name = "Name1", Description = "Description", Reference = "key1", Priority = "Medium", Status = "In progress"},
                new Epic {Name = "Name2", Description = "Description", Reference = "key2", Priority = "Medium", Status = "Done"},
                new Epic {Name = "Name3", Description = "Description", Reference = "key3", Priority = "Medium", Status = "In progress"},
                new Epic {Name = "Name4", Description = "Description", Reference = "key4", Priority = "Medium", Status = "Done"},
            });

            MockJiraSettings.Setup(x => x.GetJiraStatusFromMapping(It.IsAny<string>(), It.IsAny<string>(), "In progress")).Returns("Not done!");
            MockJiraSettings.Setup(x => x.GetJiraStatusFromMapping(It.IsAny<string>(), It.IsAny<string>(), "Done")).Returns("Done");
            MockJira.Setup(x => x.GetIssueTransitionId(It.IsAny<string>(), It.IsAny<string>())).Returns("3");
            MockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(new SearchResult
            {
                issues = new List<Issue>
                {
                    new Issue {Key = "key", Fields = new Fields {Summary = "Name", Description = "Description", Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                    new Issue {Key = "key1", Fields = new Fields {Summary = "Name1", Description = "Description", Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                    new Issue {Key = "key2", Fields = new Fields {Summary = "Name2", Description = "Description" , Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                    new Issue {Key = "key3", Fields = new Fields {Summary = "Name3", Description = "Description", Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                    new Issue {Key = "key4", Fields = new Fields {Summary = "Name4", Description = "Description" , Status = new Status {Name = "Not done!"}, Priority = new Priority{Id = "3"}}},
                }
            });
            MockJira.SetupGet(x => x.InstanceUrl).Returns("http://jira-6.cloudapp.net:8080");

            _worker = new EpicWorker(MockV1.Object, MockLogger.Object);

            await _worker.UpdateEpics(MockJira.Object);
        }

        [TestMethod]
        public void calls_GetEpicWithReference_once()
        {
            MockV1.Verify(x => x.GetEpicsWithReference(ProjectId, EpicCategory), Times.Once);
        }

        [TestMethod]
        public void calls_GetEpicsInProject_once()
        {
            MockJira.Verify(x => x.GetEpicsInProject(JiraKey), Times.Once);
        }

        [TestMethod]
        public void calls_RunTransitionOnIssue_three_times()
        {
            MockJira.Verify(x => x.RunTransitionOnIssue("3", It.IsAny<string>()), Times.Exactly(3));
        }
    }
}
