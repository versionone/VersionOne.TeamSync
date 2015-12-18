using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.Interfaces;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraWorker;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.VersionOne.Domain;

namespace VersionOne.TeamSync.Core.Tests
{

    [TestClass]
    public class and_epic_has_no_status_set : Worker_when_there_is_a_new_epic_in_v1
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
    public class and_epic_has_status_set : Worker_when_there_is_a_new_epic_in_v1
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

            _worker = new EpicWorker(MockV1.Object, MockV1Log.Object);

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

    [TestClass]
    public class and_new_story_has_status_set : story_bits
    {
        [TestInitialize]
        public void Context()
        {
            BuildContext();
            MockV1.Setup(x => x.GetStatusIdFromName("To Do")).ReturnsAsync("StoryStatus:133");
            MockJiraSettings.Setup(x => x.GetV1StatusFromMapping(It.IsAny<string>(), It.IsAny<string>(), "To Do"))
                .Returns("ToDo");
            NewIssue.Fields.EpicLink = null;
            
            Worker = new StoryWorker(MockV1.Object, MockV1Log.Object);
            Worker.CreateStories(MockJira.Object, new List<Issue> { NewIssue }, new List<Story>());
        }
        
        [TestMethod]
        public void call_GetV1StatusFromMapping_once()
        {
            MockJiraSettings.Verify(x => x.GetV1StatusFromMapping(It.IsAny<string>(), It.IsAny<string>(), "To Do"), Times.Once);
        }

        [TestMethod]
        public void call_GetStatusIdFromName_once()
        {
            MockV1.Verify(x => x.GetStatusIdFromName("ToDo"), Times.Once);
        }

        [TestMethod]
        public void call_CreateStory_once()
        {
            MockV1.Verify(x => x.CreateStory(It.IsAny<Story>()), Times.Once);
        }
    }

    [TestClass]
    public class and_existing_story_has_new_status_set : story_bits
    {
        [TestInitialize]
        public void Context()
        {
            BuildContext();
            MockV1.Setup(x => x.GetEpicsWithReference(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Epic>());
            MockV1.Setup(x => x.GetStatusIdFromName("In Progress")).ReturnsAsync("StoryStatus:134");
            MockJiraSettings.Setup(x => x.GetV1StatusFromMapping(It.IsAny<string>(), It.IsAny<string>(), "In progress"))
                .Returns("In Progress");

            ExistingStory.Status = "To Do";
            ExistingIssue.Fields.Status = new Status{Name = "In progress"};

            Worker = new StoryWorker(MockV1.Object, MockV1Log.Object);
            Worker.UpdateStories(MockJira.Object, new List<Issue> { ExistingIssue }, new List<Story>{ExistingStory});
        }

        [TestMethod]
        public void call_GetV1StatusFromMapping_once()
        {
            MockJiraSettings.Verify(x => x.GetV1StatusFromMapping(It.IsAny<string>(), It.IsAny<string>(), "In progress"), Times.Once);
        }

        [TestMethod]
        public void call_GetStatusIdFromName_once()
        {
            MockV1.Verify(x => x.GetStatusIdFromName("In Progress"), Times.Once);
        }

        [TestMethod]
        public void call_CreateStory_once()
        {
            MockV1.Verify(x => x.UpdateAsset(It.IsAny<IV1Asset>(), It.IsAny<XDocument>()), Times.Once);
        }
    }
}
