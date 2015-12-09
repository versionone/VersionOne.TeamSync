using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraWorker;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.V1Connector.Interfaces;

namespace VersionOne.TeamSync.Core.Tests.StorySync
{
    [TestClass]
    public class take_jira_story_to_v1_story : worker_bits
    {
        private const string IssueKey = "OPC-71";
        private StoryWorker _worker;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            MockV1.Setup(x => x.CreateStory(It.IsAny<Story>())).ReturnsAsync(new Story());
            _worker = new StoryWorker(MockV1.Object, MockLogger.Object);
            await _worker.CreateStoryFromJira(MockJira.Object, new Issue
            {
                Key = IssueKey,
                RenderedFields = new RenderedFields { Description = "descript" },
                Fields = new Fields
                {
                    Priority = new Priority { Name = "Medium" },
                    Status = new Status { Name = "To Do" }
                }
            });
        }

        [TestMethod]
        public void should_not_add_it_to_the_v1_project()
        {
            MockV1.Verify(x => x.CreateStory(It.IsAny<Story>()), Times.Once);
        }

        [TestMethod]
        public void should_call_add_a_label_to_jira_with_the_newly_created_story()
        {
            MockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), IssueKey), Times.Once);
        }

        [TestMethod]
        public void should_call_refresh_basic_info_for_newly_created_story()
        {
            MockV1.Verify(x => x.RefreshBasicInfo(It.IsAny<Story>()), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_add_a_comment_back_to_jira()
        {
            MockJira.Verify(x => x.AddComment(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_link_back_to_jira()
        {
            MockJira.Verify(x => x.AddWebLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
    }

    [TestClass]
    [DefectNumber("D-09878")]
    public class take_jira_story_to_v1_story_and_epic_is_closed : worker_bits
    {
        private const string IssueKey = "OPC-71";
        private StoryWorker _worker;
        private Story _createdStory;

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            MockV1.Setup(x => x.CreateStory(It.IsAny<Story>()))
                .Callback((Story story) =>
                {
                    _createdStory = story;
                })
                .ReturnsAsync(_createdStory);

            MockV1.Setup(x => x.GetAssetIdFromJiraReferenceNumber("Epic", "E-1000"))
                .ReturnsAsync(new BasicAsset { AssetState = "128" });
            _worker = new StoryWorker(MockV1.Object, MockLogger.Object);
            await _worker.CreateStoryFromJira(MockJira.Object, new Issue
            {
                Key = IssueKey,
                RenderedFields = new RenderedFields { Description = "descript" },
                Fields = new Fields
                {
                    EpicLink = "E-1000",
                    Priority = new Priority { Name = "Low" },
                    Status = new Status { Name = "Done" }
                }
            });
        }

        [TestMethod]
        public void should_call_create_story_once()
        {
            MockV1.Verify(x => x.CreateStory(It.IsAny<Story>()), Times.Never);
        }

        [TestMethod]
        public void should_log_an_error()
        {
            MockLogger.Verify(x => x.Error("Unable to assign epic E-1000 -- Epic may be closed"));
        }

        [TestMethod]
        public void should_not_update_the_issue()
        {
            MockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), IssueKey), Times.Never);
        }

        [TestMethod]
        public void should_not_call_refresh_info()
        {
            MockV1.Verify(x => x.RefreshBasicInfo(It.IsAny<Story>()), Times.Never);
        }

        [TestMethod]
        public void does_not_create_a_comment()
        {
            MockJira.Verify(x => x.AddComment(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void does_not_try_to_add_weblink()
        {
            MockJira.Verify(x => x.AddWebLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    public abstract class update_jira_story_to_v1 : worker_bits
    {
        protected const string IssueKey = "OPC-71";
        protected const string StoryId = "Story:1000";
        protected Status Status;
        protected Epic Epic = new Epic();
        protected Story Story = new Story();
        protected Story UpdateStory;
        private StoryWorker _worker;

        public async void Context()
        {
            BuildContext();
            MockV1.Setup(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()))
                .Callback<IV1Asset, XDocument>((story, doc) =>
                {
                    UpdateStory = (Story)story;
                })
                .ReturnsAsync(new XDocument());
            MockV1.Setup(x => x.GetReferencedEpic(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Epic);

            var data = new Dictionary<string, int>();
            data["reopened"] = 0;
            data["updated"] = 0;
            data["closed"] = 0;

            Story.ID = StoryId;
            _worker = new StoryWorker(MockV1.Object, MockLogger.Object);
            await _worker.UpdateStoryFromJiraToV1(MockJira.Object, new Issue
            {
                Key = IssueKey,
                RenderedFields = new RenderedFields
                {
                    Description = "descript"
                },
                Fields = new Fields
                {
                    Status = Status,
                    Summary = "summary",
                    Priority = new Priority { Name = "Medium" },
                    EpicLink = Epic.Number
                }
            }, Story, data);
        }
    }

    [TestClass]
    public class and_the_status_is_normal : update_jira_story_to_v1
    {
        [TestInitialize]
        public void Setup()
        {
            Story.AssetState = "64";
            Status = new Status { Name = "In Progress" };
            Context();
        }

        [TestMethod]
        public void should_not_add_it_to_the_v1_project()
        {
            MockV1.Verify(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_pass_along_data_to_update_story()
        {
            UpdateStory.ID.ShouldEqual(StoryId);
            UpdateStory.Reference.ShouldEqual(IssueKey);
        }

        [TestMethod]
        public void should_not_call_either_operations_to_close_or_reopen()
        {
            MockV1.Verify(x => x.CloseStory(It.IsAny<string>()), Times.Never);
            MockV1.Verify(x => x.ReOpenStory(It.IsAny<string>()), Times.Never);
        }
    }

    [TestClass]
    public class and_the_status_is_closed_when_the_v1_story_is_open : update_jira_story_to_v1
    {
        [TestInitialize]
        public void Setup()
        {
            Story.AssetState = "64";
            Status = new Status { Name = "Done" };
            Context();
        }

        [TestMethod]
        public void should_call_close_story()
        {
            MockV1.Verify(x => x.CloseStory(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void should_not_call_reopen_story()
        {
            MockV1.Verify(x => x.ReOpenStory(It.IsAny<string>()), Times.Never);
        }
    }
    [TestClass]
    public class and_the_status_is_not_done_when_the_v1_story_is_closed : update_jira_story_to_v1
    {
        [TestInitialize]
        public void Setup()
        {
            Story.AssetState = "128";
            Status = new Status { Name = "ToDo" };
            Context();
        }

        [TestMethod]
        public void should_not_call_close_story()
        {
            MockV1.Verify(x => x.CloseStory(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void should_call_reopen_story()
        {
            MockV1.Verify(x => x.ReOpenStory(It.IsAny<string>()), Times.Once);
        }
    }

    [TestClass]
    [DefectNumber("D-09878")]
    public class and_the_status_is_normal_but_the_parent_epic_is_closed : update_jira_story_to_v1
    {
        [TestInitialize]
        public void Setup()
        {
            Epic.AssetState = "128";
            Epic.ID = "1000";
            Epic.Number = "E-1000";
            Story.AssetState = "64";
            Story.Super = "Epic:2000";
            Story.SuperNumber = "E-2000";

            Status = new Status() { Name = "In Progress" };
            Context();
        }

        [TestMethod]
        public void should_not_call_either_operations_to_close_or_reopen()
        {
            MockV1.Verify(x => x.CloseStory(It.IsAny<string>()), Times.Never);
            MockV1.Verify(x => x.ReOpenStory(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void should_not_update_the_parent_epic() //null value means no update
        {
            UpdateStory.Super.ShouldBeNull();
        }

        [TestMethod]
        public void should_log_a_message_about_the_closed_epic()
        {
            MockLogger.Verify(x => x.Error("Cannot assign a story to a closed Epic.  Story will be still be updated, but reassign to an open Epic"), Times.Once);
        }
    }
}