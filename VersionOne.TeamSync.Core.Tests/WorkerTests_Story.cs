using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class story_update : story_bits
    {
        [TestInitialize]
        public void Context()
        {
            BuildContext();
        }

        [TestMethod]
        public void should_call_update_asset_just_one_time()
        {
            var jiraInfo = MakeInfo();
            _worker.UpdateStories(jiraInfo, new List<Issue> { _existingIssue, _newIssue }, new List<Story> { _existingStory });
            _mockV1.Verify(x => x.UpdateAsset(It.IsAny<Story>(),It.IsAny<XDocument>()), Times.Once);
        }

    }

    public abstract class story_bits : worker_bits
    {
        protected Story _existingStory;
        protected Story _fakeCreatedStory;
        protected Issue _newIssue;
        protected Issue _existingIssue;
        protected SearchResult _searchResult;
        protected string _storyNumber = "S-0001";
        protected string _newIssueKey = "OPC-15";
        protected string _existingIssueKey;

        protected override void BuildContext()
        {
            base.BuildContext();
            _existingIssueKey = "OPC-10";
            _existingStory = new Story() { Reference = _existingIssueKey, Name = "Johnny", Number = _storyNumber };
            _existingIssue = new Issue()
            {
                Key = _existingIssueKey,
                Fields = new Fields() { Labels = new List<string> { _storyNumber } }
            };

            _newIssue = new Issue()
            {
                Key = _newIssueKey,
                Fields = new Fields()
            };
            _fakeCreatedStory = new Story() {Number = "S-8900"};
            _mockV1.Setup(x => x.CreateStory(It.IsAny<Story>())).ReturnsAsync(_fakeCreatedStory);
        }


    }

    [TestClass]
    public class orphan_story : story_bits
    {

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            _newIssue.Fields.EpicLink = null;

            var jiraInfo = MakeInfo();
            _worker.CreateStories(jiraInfo, new List<Issue> { _existingIssue, _newIssue }, new List<Story> { _existingStory });
        }

        [TestMethod]
        public void should_call_create_asset_just_one_time()
        {

            _mockV1.Verify(x => x.CreateStory(It.IsAny<Story>()), Times.Once);
        }

        [TestMethod]
        public void should_not_try_to_get_an_epic_id()
        {
            _mockV1.Verify(x => x.GetAssetIdFromJiraReferenceNumber(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void should_refresh_the_story_once()
        {
            _mockV1.Verify(x => x.RefreshBasicInfo(_fakeCreatedStory), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_to_update_the_issue_in_jira_with_story_number()
        {
            _mockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), _newIssueKey), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_link_back_to_jira()
        {
            _mockJira.Verify(x => x.AddLinkToV1InComments(_newIssueKey, _fakeCreatedStory.Number, It.IsAny<string>(),It.IsAny<string>()), Times.Once());
        }
    }

    [TestClass]
    public class child_story : story_bits
    {
        private string _epicLink = "OPC-8";

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            _newIssue.Fields.EpicLink = _epicLink;

            var jiraInfo = MakeInfo();
            _worker.CreateStories(jiraInfo, new List<Issue> { _existingIssue, _newIssue }, new List<Story> { _existingStory });
        }

        [TestMethod]
        public void should_call_create_asset_just_one_time()
        {
            _mockV1.Verify(x => x.CreateStory(It.IsAny<Story>()), Times.Once);
        }

        [TestMethod]
        public void should_try_to_get_an_epic_id()
        {
            _mockV1.Verify(x => x.GetAssetIdFromJiraReferenceNumber(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void should_refresh_the_story_once()
        {
            _mockV1.Verify(x => x.RefreshBasicInfo(_fakeCreatedStory), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_to_update_the_issue_in_jira_with_story_number()
        {
            _mockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), _newIssueKey), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_link_back_to_jira()
        {
            _mockJira.Verify(x => x.AddLinkToV1InComments(_newIssueKey, _fakeCreatedStory.Number, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
    }

}
