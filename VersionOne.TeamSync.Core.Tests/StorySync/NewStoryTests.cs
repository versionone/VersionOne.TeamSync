using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests.StorySync
{
    [TestClass]
    public class take_jira_story_to_v1_story : worker_bits
    {
        private string _issueKey = "OPC-71";
        [TestInitialize]
        public void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.CreateStory(It.IsAny<Story>())).ReturnsAsync(new Story());
            _worker.CreateStoryFromJira(MakeInfo(), new Issue()
            {
                Key = _issueKey,
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
        public void should_call_add_a_comment_to_jira_issue()
        {
            _mockJira.Verify(x => x.AddLinkToV1InComments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }

    [TestClass]
    public class update_jira_story_to_v1 : worker_bits
    {
        private string _issueKey = "OPC-71";
        private Story _updateStory;
        private string _storyId;

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()))
                .Callback<IV1Asset, XDocument>((story,doc) =>
                {
                    _updateStory = (Story) story;
                });

            _storyId = "Story:1000";
            _worker.UpdateStoryFromJiraToV1(MakeInfo(), new Issue()
            {
                Key = _issueKey,
                Fields = new Fields()
            }, new Story(){ID = _storyId});
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
}