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
using VersionOne.TeamSync.Worker.Extensions;

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

            _updatedStory = new Story { Reference = "J-100", Name = "Johnny", Number = "S-9000", Estimate = "", ToDo = "", SuperNumber = "", Description = "" };
            _johnnyIsAlive = "Johnny 5 is alive";
            var updatedIssue = new Issue
            {
                Key = "J-100",
                RenderedFields = new RenderedFields
                {
                    Description = "a new description"
                },
                Fields = new Fields
                {
                    Summary = _johnnyIsAlive,
                    Labels = new List<string> { "S-9000" },
                    Priority = new Priority { Name = "Medium" }
                }
            };

            MockV1.Setup(x => x.GetEpicsWithReference(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Epic>());

            MockV1.Setup(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>())).Callback(
                (IV1Asset asset, XDocument xDocument) =>
                {
                    _storySentToUpdate = (Story)asset;
                }).ReturnsAsync(new XDocument());
            Worker = new StoryWorker(MockV1.Object, MockLogger.Object);

            Worker.UpdateStories(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue, updatedIssue }, new List<Story> { ExistingStory, _updatedStory });
        }

        [TestMethod]
        public void should_call_update_asset_just_one_time()
        {
            MockV1.Verify(x => x.UpdateAsset(It.IsAny<Story>(), It.IsAny<XDocument>()), Times.Once);
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

            Worker = new StoryWorker(MockV1.Object, MockLogger.Object);
        }

        [TestMethod]
        public void should_never_call_delete_asset()
        {
            // All jira stories referenced in V1 exist in Jira - No stories should be deleted
            Worker.DeleteV1Stories(MockJira.Object, _allJiraStories, _allV1Stories);
            MockV1.Verify(x => x.DeleteStoryWithJiraReference(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void should_call_delete_asset_just_one_time()
        {
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-2")));
            // OPC-2 removed - Story 3 should be deleted
            Worker.DeleteV1Stories(MockJira.Object, _allJiraStories, _allV1Stories);
            MockV1.Verify(x => x.DeleteStoryWithJiraReference(It.IsAny<string>(), "OPC-2"), Times.Once);
        }

        [TestMethod]
        public void should_call_delete_asset_just_two_times()
        {
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-1")));
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-3")));
            // OPC-1 and OPC-3 removed - Story 2 and Story 4 should be deleted
            Worker.DeleteV1Stories(MockJira.Object, _allJiraStories, _allV1Stories);
            MockV1.Verify(x => x.DeleteStoryWithJiraReference(It.IsAny<string>(), It.IsIn("OPC-1", "OPC-3")), Times.Exactly(2));
        }
    }

    public abstract class story_bits : worker_bits
    {
        protected Story ExistingStory;
        protected Story FakeCreatedStory;
        protected Issue NewIssue;
        protected Issue ExistingIssue;
        protected SearchResult SearchResult;
        protected string StoryNumber = "S-0001";
        protected string NewIssueKey = "OPC-15";
        protected string ExistingIssueKey;
        protected StoryWorker Worker;

        protected override void BuildContext()
        {
            base.BuildContext();

            ExistingIssueKey = "OPC-10";
            ExistingStory = new Story { Reference = ExistingIssueKey, Name = "Johnny", Number = StoryNumber, Description = "descript", ToDo = "", Estimate = "", SuperNumber = "", Priority = "WorkitemPriority:139" };
            ExistingIssue = new Issue
            {
                Key = ExistingIssueKey,
                RenderedFields = new RenderedFields { Description = "descript" },
                Fields = new Fields
                {
                    Labels = new List<string> { StoryNumber },
                    Summary = "Johnny",
                    Priority = new Priority { Name = "Medium" }
                }
            };

            NewIssue = new Issue
            {
                Key = NewIssueKey,
                Fields = new Fields
                {
                    Priority = new Priority { Name = "Medium" }
                },
                RenderedFields = new RenderedFields()
            };
            FakeCreatedStory = new Story { Number = "S-8900" };
            MockV1.Setup(x => x.CreateStory(It.IsAny<Story>())).ReturnsAsync(FakeCreatedStory);
            Worker = new StoryWorker(MockV1.Object, MockLogger.Object);
        }
    }

    [TestClass]
    public class orphan_story : story_bits
    {

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            NewIssue.Fields.EpicLink = null;

            Worker = new StoryWorker(MockV1.Object, MockLogger.Object);

            Worker.CreateStories(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue }, new List<Story> { ExistingStory });
        }

        [TestMethod]
        public void should_call_create_asset_just_one_time()
        {

            MockV1.Verify(x => x.CreateStory(It.IsAny<Story>()), Times.Once);
        }

        [TestMethod]
        public void should_not_try_to_get_an_epic_id()
        {
            MockV1.Verify(x => x.GetAssetIdFromJiraReferenceNumber(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void should_refresh_the_story_once()
        {
            MockV1.Verify(x => x.RefreshBasicInfo(FakeCreatedStory), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_to_update_the_issue_in_jira_with_story_number()
        {
            MockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), NewIssueKey), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_comment_back_to_jira()
        {
            MockJira.Verify(x => x.AddComment(NewIssueKey, It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_link_back_to_jira()
        {
            MockJira.Verify(x => x.AddWebLink(NewIssueKey, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
    }

    [TestClass]
    public class child_story : story_bits
    {
        private const string EpicLink = "OPC-8";

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            NewIssue.Fields.EpicLink = EpicLink;
            MockV1.Setup(x => x.GetAssetIdFromJiraReferenceNumber(It.IsAny<string>(), EpicLink))
                .ReturnsAsync(new BasicAsset(){AssetState = "64", ID = "Epic:1000"});
            Worker.CreateStories(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue }, new List<Story> { ExistingStory });
        }

        [TestMethod]
        public void should_call_create_asset_just_one_time()
        {
            MockV1.Verify(x => x.CreateStory(It.IsAny<Story>()), Times.Once);
        }

        [TestMethod]
        public void should_try_to_get_an_epic_id()
        {
            MockV1.Verify(x => x.GetAssetIdFromJiraReferenceNumber(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void should_refresh_the_story_once()
        {
            MockV1.Verify(x => x.RefreshBasicInfo(FakeCreatedStory), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_to_update_the_issue_in_jira_with_story_number()
        {
            MockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), NewIssueKey), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_comment_back_to_jira()
        {
            MockJira.Verify(x => x.AddComment(NewIssueKey, It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void makes_a_call_add_a_link_back_to_jira()
        {
            MockJira.Verify(x => x.AddWebLink(NewIssueKey, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
    }

    [TestClass]
    public class story_with_assignee : story_bits
    {
        [TestInitialize]
        public void Context()
        {
            BuildContext();
            NewIssue.Fields.EpicLink = null;
            NewIssue.Fields.Assignee = Assignee;

            MockV1.Setup(x => x.GetEpicsWithoutReference(ProjectId, EpicCategory)).ReturnsAsync(new List<Epic>());
            MockV1.Setup(x => x.SyncMemberFromJiraUser(Assignee)).ReturnsAsync(Assignee.ToV1Member());

            Worker = new StoryWorker(MockV1.Object, MockLogger.Object);

            Worker.CreateStories(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue }, new List<Story> { ExistingStory });
        }

        [TestMethod]
        public void should_sync_member_at_least_once()
        {
            MockV1.Verify(x => x.SyncMemberFromJiraUser(Assignee), Times.AtLeastOnce);
        }
    }
}
