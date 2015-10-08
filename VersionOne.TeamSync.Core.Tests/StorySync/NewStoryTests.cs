using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests.StorySync
{
    [TestClass]
    public class take_jira_story_to_v1_story : worker_bits
    {
        private StoryWorker _worker;
        private string _issueKey = "OPC-71";
        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.CreateStory(It.IsAny<Story>())).ReturnsAsync(new Story());
            _worker = new StoryWorker(_mockV1.Object, _mockLogger.Object);
            await _worker.CreateStoryFromJira(_mockJira.Object, new Issue()
            {
                Key = _issueKey,
                RenderedFields = new RenderedFields(){Description = "descript"},
                Fields = new Fields()
            });
        }

        [TestMethod]
        public void should_not_add_it_to_the_v1_project()
        {
            _mockV1.Verify(x => x.CreateStory(It.IsAny<Story>()), Times.Once);
        }

        [TestMethod]
        public void should_call_add_a_label_to_jira_with_the_newly_created_story()
        {
            _mockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), _issueKey), Times.Once);
        }

        [TestMethod]
        public void should_call_refresh_basic_info_for_newly_created_story()
        {
            _mockV1.Verify(x => x.RefreshBasicInfo(It.IsAny<Story>()), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_add_a_comment_back_to_jira()
        {
            _mockJira.Verify(x => x.AddComment(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_link_back_to_jira()
        {
            _mockJira.Verify(x => x.AddWebLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
    }

    public abstract class update_jira_story_to_v1 : worker_bits
    {
        private string _issueKey = "OPC-71";
        private Story _updateStory;
        private string _storyId = "Story:1000";
        protected Status _status;
        protected Story _story = new Story();
        private StoryWorker _worker;

        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()))
                .Callback<IV1Asset, XDocument>((story,doc) =>
                {
                    _updateStory = (Story) story;
                })
                .ReturnsAsync(new XDocument());
            
            var data = new Dictionary<string, int>();
            data["reopened"] = 0;
            data["updated"] = 0;
            data["closed"] = 0;

            _story.ID = _storyId;
            _worker = new StoryWorker(_mockV1.Object, _mockLogger.Object);
            await _worker.UpdateStoryFromJiraToV1(_mockJira.Object, new Issue()
            {
                Key = _issueKey,
                RenderedFields = new RenderedFields()
                {
                    Description = "descript"
                },
                Fields = new Fields()
                {
                    Status = _status,
                    Summary = "summary",
                }
            }, _story, new List<Epic>(), data);
        }

        [TestMethod]
        public void should_not_add_it_to_the_v1_project()
        {
            _mockV1.Verify(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_pass_along_data_to_update_story()
        {
            _updateStory.ID.ShouldEqual(_storyId);
            _updateStory.Reference.ShouldEqual(_issueKey);
        }
    }

    [TestClass]
    public class and_the_status_is_normal : update_jira_story_to_v1
    {
        [TestInitialize]
        public void Setup()
        {
            _story.AssetState = "64";
            _status = new Status(){Name = "In Progress"};
            Context();
        }

        [TestMethod]
        public void should_not_call_either_operations_to_close_or_reopen()
        {
            _mockV1.Verify(x => x.CloseStory(It.IsAny<string>()), Times.Never);
            _mockV1.Verify(x => x.ReOpenStory(It.IsAny<string>()), Times.Never);
        }
    }

    [TestClass]
    public class and_the_status_is_closed_when_the_v1_story_is_open : update_jira_story_to_v1
    {
        [TestInitialize]
        public void Setup()
        {
            _story.AssetState = "64";
            _status = new Status() { Name = "Done" }; 
            Context();
        }

        [TestMethod]
        public void should_call_close_story()
        {
            _mockV1.Verify(x => x.CloseStory(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void should_not_call_reopen_story()
        {
            _mockV1.Verify(x => x.ReOpenStory(It.IsAny<string>()), Times.Never);
        }
    }
    [TestClass]
    public class and_the_status_is_not_done_when_the_v1_story_is_closed : update_jira_story_to_v1
    {
        [TestInitialize]
        public void Setup()
        {
            _story.AssetState = "128";
            _status = new Status(){Name = "ToDo"};
            Context();
        }

        [TestMethod]
        public void should_not_call_close_story()
        {
            _mockV1.Verify(x => x.CloseStory(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void should_call_reopen_story()
        {
            _mockV1.Verify(x => x.ReOpenStory(It.IsAny<string>()), Times.Once);
        }
    }
}