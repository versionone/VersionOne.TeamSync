using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Config;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker;
using VersionOne.TeamSync.Worker.Domain;

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
                    Priority = new Priority { Name = "Medium" }
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

    public abstract class update_jira_story_to_v1 : worker_bits
    {
        private const string IssueKey = "OPC-71";
        private const string StoryId = "Story:1000";
        protected Status Status;
        protected Story Story = new Story();
        private Story _updateStory;
        private StoryWorker _worker;

        public async void Context()
        {
            BuildContext();
            MockV1.Setup(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()))
                .Callback<IV1Asset, XDocument>((story, doc) =>
                {
                    _updateStory = (Story)story;
                })
                .ReturnsAsync(new XDocument());

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
                    Priority = new Priority { Name = "Medium" }
                }
            }, Story, new List<Epic>(), data);
        }

        [TestMethod]
        public void should_not_add_it_to_the_v1_project()
        {
            MockV1.Verify(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_pass_along_data_to_update_story()
        {
            _updateStory.ID.ShouldEqual(StoryId);
            _updateStory.Reference.ShouldEqual(IssueKey);
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
}