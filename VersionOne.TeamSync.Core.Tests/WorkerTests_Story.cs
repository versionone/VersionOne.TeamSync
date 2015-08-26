using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.Worker;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class story_update : story_bits
    {
        private Story _updatedStory;
        private Story _storySentToUpdate;
        private string _johnnyIsAlive;

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            var jiraInfo = MakeInfo();

            _updatedStory = new Story() { Reference = "J-100", Name = "Johnny", Number = "S-9000", Estimate = "", ToDo = "", SuperNumber = "", Description = ""};
            _johnnyIsAlive = "Johnny 5 is alive";
            var updatedIssue = new Issue()
            {
                Key = "J-100",
                RenderedFields = new RenderedFields()
                {
                    Description = "a new description"
                },
                Fields = new Fields()
                {
                    Summary = _johnnyIsAlive,
                    Labels = new List<string> { "S-9000" }
                }
            };

            _mockV1.Setup(x => x.GetEpicsWithReference(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Epic>());

            _mockV1.Setup(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>())).Callback(
                (IV1Asset asset, XDocument xDocument) =>
                {
                    _storySentToUpdate = (Story)asset;
                }).ReturnsAsync(new XDocument());
            _worker = new StoryWorker(_mockV1.Object, _mockLogger.Object);

            _worker.UpdateStories(jiraInfo, new List<Issue> { _existingIssue, _newIssue, updatedIssue }, new List<Story> { _existingStory, _updatedStory });
        }

        [TestMethod]
        public void should_call_update_asset_just_one_time()
        {
            _mockV1.Verify(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_send_the_right_story_to_be_updated()
        {
            _storySentToUpdate.Name.ShouldEqual(_johnnyIsAlive);
        }

    }

    [TestClass]
    public class story_delete : story_bits
    {
        private List<Issue> _allJiraStories;
        private List<Story> _allV1Stories;

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            _allJiraStories = new List<Issue>
                {
                    new Issue
                    {
                        Key = "OPC-1",
                        Fields = new Fields()
                    },
                    new Issue
                    {
                        Key = "OPC-2",
                        Fields = new Fields()
                    },
                    new Issue
                    {
                        Key = "OPC-3",
                        Fields = new Fields()
                    }
                };

            _allV1Stories = new List<Story>
                {
                    new Story
                    {
                        Name = "Story 1",
                        Number = "S-00001"
                    },
                    new Story
                    {
                        Name = "Story 2",
                        Number = "S-00002",
                        Reference = "OPC-1"
                    },
                    new Story
                    {
                        Name = "Story 3",
                        Number = "S-00003",
                        Reference = "OPC-2"
                    },
                    new Story
                    {
                        Name = "Story 4",
                        Number = "S-00004",
                        Reference = "OPC-3"
                    }
                };

            _worker = new StoryWorker(_mockV1.Object, _mockLogger.Object);

        }

        [TestMethod]
        public void should_never_call_delete_asset()
        {
            // All jira stories referenced in V1 exist in Jira - No stories should be deleted
            var jiraInfo = MakeInfo();

            _worker.DeleteV1Stories(jiraInfo, _allJiraStories, _allV1Stories);
            _mockV1.Verify(x => x.DeleteStoryWithJiraReference(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void should_call_delete_asset_just_one_time()
        {
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-2")));
            // OPC-2 removed - Story 3 should be deleted
            var jiraInfo = MakeInfo();
            _worker.DeleteV1Stories(jiraInfo, _allJiraStories, _allV1Stories);
            _mockV1.Verify(x => x.DeleteStoryWithJiraReference(It.IsAny<string>(), "OPC-2"), Times.Once);
        }

        [TestMethod]
        public void should_call_delete_asset_just_two_times()
        {
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-1")));
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-3")));
            // OPC-1 and OPC-3 removed - Story 2 and Story 4 should be deleted
            var jiraInfo = MakeInfo();
            _worker.DeleteV1Stories(jiraInfo, _allJiraStories, _allV1Stories);
            _mockV1.Verify(x => x.DeleteStoryWithJiraReference(It.IsAny<string>(), It.IsIn("OPC-1", "OPC-3")), Times.Exactly(2));
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
        protected StoryWorker _worker;


        protected override void BuildContext()
        {
            base.BuildContext();
            _existingIssueKey = "OPC-10";
            _existingStory = new Story { Reference = _existingIssueKey, Name = "Johnny", Number = _storyNumber, Description = "descript", ToDo = "", Estimate = "", SuperNumber = ""};
            _existingIssue = new Issue
            {
                Key = _existingIssueKey,
                RenderedFields = new RenderedFields { Description = "descript" },
                Fields = new Fields
                {
                    Labels = new List<string> { _storyNumber },
                    Summary = "Johnny"
                }
            };

            _newIssue = new Issue
            {
                Key = _newIssueKey,
                Fields = new Fields(),
                RenderedFields = new RenderedFields()
            };
            _fakeCreatedStory = new Story { Number = "S-8900" };
            _mockV1.Setup(x => x.CreateStory(It.IsAny<Story>())).ReturnsAsync(_fakeCreatedStory);
            _worker = new StoryWorker(_mockV1.Object, _mockLogger.Object);
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
            _worker = new StoryWorker(_mockV1.Object, _mockLogger.Object);

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
        public void makes_a_call_add_a_comment_back_to_jira()
        {
            _mockJira.Verify(x => x.AddComment(_newIssueKey, It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_link_back_to_jira()
        {
            _mockJira.Verify(x => x.AddWebLink(_newIssueKey, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
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
        public void makes_a_call_add_a_comment_back_to_jira()
        {
            _mockJira.Verify(x => x.AddComment(_newIssueKey, It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_link_back_to_jira()
        {
            _mockJira.Verify(x => x.AddWebLink(_newIssueKey, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
    }
}
