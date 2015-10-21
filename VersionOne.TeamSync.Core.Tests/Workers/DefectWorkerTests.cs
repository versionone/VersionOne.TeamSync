
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

namespace VersionOne.TeamSync.Core.Tests.Workers
{
    [TestClass]
    public class defect_update : defect_bits
    {
        private Defect _updatedDefect;
        private Defect _defectSentToUpdate;
        private string _johnnyIsAlive;

        [TestInitialize]
        public void Context()
        {
            BuildContext();

            _updatedDefect = new Defect { Reference = "J-100", Name = "Johnny", Number = "S-9000", Estimate = "", ToDo = "", SuperNumber = "", Description = "" };
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

            MockV1.Setup(x => x.UpdateAsset(It.IsAny<Defect>(), It.IsAny<XDocument>())).Callback(
                (IV1Asset asset, XDocument xDocument) =>
                {
                    _defectSentToUpdate = (Defect)asset;
                }).ReturnsAsync(new XDocument());
            Worker = new DefectWorker(MockV1.Object, MockLogger.Object);

            Worker.UpdateDefects(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue, updatedIssue }, new List<Defect> { ExistingDefect, _updatedDefect });
        }

        [TestMethod]
        public void should_call_update_asset_just_one_time()
        {
            MockV1.Verify(x => x.UpdateAsset(It.IsAny<Defect>(), It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_send_the_right_defect_to_be_updated()
        {
            _defectSentToUpdate.Name.ShouldEqual(_johnnyIsAlive);
        }

    }

    [TestClass]
    public class defect_delete : defect_bits
    {
        private List<Issue> _allJiraStories;
        private List<Defect> _allV1Defects;

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

            _allV1Defects = new List<Defect>
                {
                    new Defect
                    {
                        Name = "Story 1",
                        Number = "S-00001"
                    },
                    new Defect
                    {
                        Name = "Story 2",
                        Number = "S-00002",
                        Reference = "OPC-1"
                    },
                    new Defect
                    {
                        Name = "Story 3",
                        Number = "S-00003",
                        Reference = "OPC-2"
                    },
                    new Defect
                    {
                        Name = "Story 4",
                        Number = "S-00004",
                        Reference = "OPC-3"
                    }
                };

            Worker = new DefectWorker(MockV1.Object, MockLogger.Object);

        }

        [TestMethod]
        public void should_never_call_delete_asset()
        {
            // All jira stories referenced in V1 exist in Jira - No stories should be deleted
            Worker.DeleteV1Defects(MockJira.Object, _allJiraStories, _allV1Defects);
            MockV1.Verify(x => x.DeleteDefectWithJiraReference(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void should_call_delete_asset_just_one_time()
        {
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-2")));
            // OPC-2 removed - Story 3 should be deleted
            Worker.DeleteV1Defects(MockJira.Object, _allJiraStories, _allV1Defects);
            MockV1.Verify(x => x.DeleteDefectWithJiraReference(It.IsAny<string>(), "OPC-2"), Times.Once);
        }

        [TestMethod]
        public void should_call_delete_asset_just_two_times()
        {
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-1")));
            _allJiraStories.Remove(_allJiraStories.First(s => s.Key.Equals("OPC-3")));
            // OPC-1 and OPC-3 removed - Story 2 and Story 4 should be deleted
            Worker.DeleteV1Defects(MockJira.Object, _allJiraStories, _allV1Defects);
            MockV1.Verify(x => x.DeleteDefectWithJiraReference(It.IsAny<string>(), It.IsIn("OPC-1", "OPC-3")), Times.Exactly(2));
        }
    }

    public abstract class defect_bits : worker_bits
    {
        protected User Assignee;
        protected Defect ExistingDefect;
        protected Defect FakeCreatedStory;
        protected Issue NewIssue;
        protected Issue ExistingIssue;
        protected SearchResult SearchResult;
        protected string DefectNumber = "S-0001";
        protected string NewIssueKey = "OPC-15";
        protected string ExistingIssueKey;
        protected DefectWorker Worker;

        protected override void BuildContext()
        {
            base.BuildContext();

            Assignee = new User
            {
                displayName = "Administrator",
                name = "admin",
                emailAddress = "admin@versionone.com"
            };
            ExistingIssueKey = "OPC-10";
            ExistingDefect = new Defect { Reference = ExistingIssueKey, Name = "Johnny", Number = DefectNumber, Description = "descript", ToDo = "", Estimate = "", SuperNumber = "" };
            ExistingIssue = new Issue
            {
                Key = ExistingIssueKey,
                RenderedFields = new RenderedFields { Description = "descript" },
                Fields = new Fields
                {
                    Labels = new List<string> { DefectNumber },
                    Summary = "Johnny"
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
            FakeCreatedStory = new Defect { Number = "S-8900" };
            MockV1.Setup(x => x.CreateDefect(It.IsAny<Defect>())).ReturnsAsync(FakeCreatedStory);
            Worker = new DefectWorker(MockV1.Object, MockLogger.Object);
        }
    }

    [TestClass]
    public class orphan_defect : defect_bits
    {

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            NewIssue.Fields.EpicLink = null;

            Worker = new DefectWorker(MockV1.Object, MockLogger.Object);

            Worker.CreateDefects(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue }, new List<Defect> { ExistingDefect });
        }

        [TestMethod]
        public void should_call_create_asset_just_one_time()
        {
            MockV1.Verify(x => x.CreateDefect(It.IsAny<Defect>()), Times.Once);
        }

        [TestMethod]
        public void should_not_try_to_get_an_epic_id()
        {
            MockV1.Verify(x => x.GetAssetIdFromJiraReferenceNumber(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void should_refresh_the_defect_once()
        {
            MockV1.Verify(x => x.RefreshBasicInfo(FakeCreatedStory), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_to_update_the_issue_in_jira_with_defect_number()
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
    public class child_defect : defect_bits
    {
        private const string EpicLink = "OPC-8";

        [TestInitialize]
        public void Context()
        {
            BuildContext();
            NewIssue.Fields.EpicLink = EpicLink;

            Worker.CreateDefects(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue }, new List<Defect> { ExistingDefect });
        }

        [TestMethod]
        public void should_call_create_asset_just_one_time()
        {
            MockV1.Verify(x => x.CreateDefect(It.IsAny<Defect>()), Times.Once);
        }

        [TestMethod]
        public void should_try_to_get_an_epic_id()
        {
            MockV1.Verify(x => x.GetAssetIdFromJiraReferenceNumber(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void should_refresh_the_defect_once()
        {
            MockV1.Verify(x => x.RefreshBasicInfo(FakeCreatedStory), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_to_update_the_issue_in_jira_with_defect_number()
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
    public class defect_with_assignee : defect_bits
    {
        [TestInitialize]
        public void Context()
        {
            BuildContext();
            NewIssue.Fields.EpicLink = null;
            NewIssue.Fields.Assignee = Assignee;

            MockV1.Setup(x => x.GetEpicsWithoutReference(ProjectId, EpicCategory)).ReturnsAsync(new List<Epic>());
            MockV1.Setup(x => x.SyncMemberFromJiraUser(Assignee)).ReturnsAsync(Assignee.ToV1Member());

            Worker = new DefectWorker(MockV1.Object, MockLogger.Object);

            Worker.CreateDefects(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue }, new List<Defect> { ExistingDefect });
        }

        [TestMethod]
        public void should_sync_member_at_least_once()
        {
            MockV1.Verify(x => x.SyncMemberFromJiraUser(Assignee), Times.AtLeastOnce);
        }
    }
}
