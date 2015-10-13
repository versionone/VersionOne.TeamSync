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
        protected Defect ExistingDefect;
        protected Defect FakeCreatedDefect;
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
            MockV1.Setup(x => x.CreateDefect(It.IsAny<Defect>())).ReturnsAsync(FakeCreatedDefect);
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
        public void should_tell_us_about_creating_a_defect()
        {
            _mockLogger.Verify(x => x.Info("Created 1 V1 defects"), Times.Once);
        }

        [TestMethod]
        public void should_give_us_a_count_of_the_defects_created()
        {
            _mockLogger.Verify(x => x.DebugFormat("Found {0} defects to check for create", 1), Times.Once);
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
            MockV1.Verify(x => x.RefreshBasicInfo(FakeCreatedDefect), Times.Once);
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
        public void should_tell_us_about_creating_a_defect()
        {
            _mockLogger.Verify(x => x.Info("Created 1 V1 defects"), Times.Once);
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
            MockV1.Verify(x => x.RefreshBasicInfo(FakeCreatedDefect), Times.Once);
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
    [DefectNumber("D-09878")]
    public class defect_update_with_closed_epic : update_jira_bug_to_v1
    {
        [TestInitialize]
        public void Setup()
        {
            _epic.AssetState = "128";
            _epic.ID = "1000";
            _epic.Number = "E-1000";
            _defect.AssetState = "64";
            _defect.Super = "Epic:2000";
            _defect.SuperNumber = "E-2000";

            _status = new Status() { Name = "In Progress" };
            Context();
        }

        [TestMethod]
        public void should_not_call_either_operations_to_close_or_reopen()
        {
            _mockV1.Verify(x => x.CloseStory(It.IsAny<string>()), Times.Never);
            _mockV1.Verify(x => x.ReOpenStory(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void should_not_update_the_parent_epic() //null value means no update
        {
            _updateDefect.Super.ShouldBeNull();
        }

        [TestMethod]
        public void should_log_a_message_about_the_closed_epic()
        {
            _mockLogger.Verify(x => x.Error("Cannot assign a defect to a closed Epic.  The defect will be still be updated, but should be reassigned to an open Epic"), Times.Once);
        }

    }

    [TestClass]
    [DefectNumber("D-09878")]
    public class take_jira_bug_to_v1_defect_and_epic_is_closed : worker_bits
    {
        private DefectWorker _worker;
        private Defect _createdDefect;
        private string _issueKey = "OPC-71";

        [TestInitialize]
        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.CreateDefect(It.IsAny<Defect>()))
                .Callback((Defect story) =>
                {
                    _createdDefect = story;
                })
                .ReturnsAsync(_createdDefect);

            _mockV1.Setup(x => x.GetAssetIdFromJiraReferenceNumber("Epic", "E-1000"))
                .ReturnsAsync(new BasicAsset(){AssetState = "128"});
            _worker = new DefectWorker(_mockV1.Object, _mockLogger.Object);
            await _worker.CreateDefectFromJira(MakeInfo(), new Issue()
            {
                Key = _issueKey,
                RenderedFields = new RenderedFields() { Description = "descript" },
                Fields = new Fields()
                {
                    EpicLink = "E-1000"
                }
            });
        }

        [TestMethod]
        public void should_call_create_story_once()
        {
            _mockV1.Verify(x => x.CreateDefect(It.IsAny<Defect>()), Times.Never);
        }

        [TestMethod]
        public void should_log_an_error()
        {
            _mockLogger.Verify(x => x.Error("Unable to assign epic E-1000 -- Epic may be closed"));
        }

        [TestMethod]
        public void should_not_update_the_issue()
        {
            _mockJira.Verify(x => x.UpdateIssue(It.IsAny<Issue>(), _issueKey), Times.Never);
        }

        [TestMethod]
        public void should_not_call_refresh_info()
        {
            _mockV1.Verify(x => x.RefreshBasicInfo(It.IsAny<Defect>()), Times.Never);
        }

        [TestMethod]
        public void does_not_create_a_comment()
        {
            _mockJira.Verify(x => x.AddComment(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void does_not_try_to_add_weblink()
        {
            _mockJira.Verify(x => x.AddWebLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    public abstract class update_jira_bug_to_v1 : worker_bits
    {
        private string _issueKey = "OPC-71";
        protected Defect _updateDefect;
        private string _defectId = "Defect:1000";
        protected Status _status;
        protected Defect _defect = new Defect();
        protected Epic _epic = new Epic() { AssetState = "64" };
        private DefectWorker _worker;

        public async void Context()
        {
            BuildContext();
            _mockV1.Setup(x => x.UpdateAsset(It.IsAny<Defect>(), It.IsAny<XDocument>()))
                .Callback<IV1Asset, XDocument>((defect, doc) =>
                {
                    _updateDefect = (Defect)defect;
                })
                .ReturnsAsync(new XDocument());

            _defect.ID = _defectId;
            _worker = new DefectWorker(_mockV1.Object, _mockLogger.Object);
            await _worker.UpdateDefectFromJiraToV1(MakeInfo(), new Issue()
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
            }, _defect, new List<Epic>() { _epic });
        }

        [TestMethod]
        public void should_update_the_asset_once()
        {
            _mockV1.Verify(x => x.UpdateAsset(It.IsAny<Defect>(), It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_pass_along_data_to_update_story()
        {
            _updateDefect.ID.ShouldEqual(_defectId);
            _updateDefect.Reference.ShouldEqual(_issueKey);
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
