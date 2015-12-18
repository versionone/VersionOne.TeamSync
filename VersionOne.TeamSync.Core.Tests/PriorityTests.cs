using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.Core.Tests.Workers;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraConnector.Interfaces;
using VersionOne.TeamSync.JiraWorker;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.VersionOne.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    public abstract class When_trying_to_post_a_new_epic_on_jira_without_priority : worker_bits
    {
        protected Mock<IJiraConnector> MockJiraConnector;
        protected Epic Epic;
        protected ItemBase ItemBase;
        protected MetaProject ProjectMeta;
        protected IJira Jira;

        protected virtual void BuildPriorityContext()
        {
            BuildContext();

            Epic = new Epic { Number = "5", Description = "descript", Name = "Johnny", ScopeName = "v1" };
            ItemBase = new ItemBase { Key = JiraKey };

            MockJiraConnector = new Mock<IJiraConnector>();
            MockJiraConnector.Setup(
                x => x.Post<ItemBase>(It.IsAny<string>(), It.IsAny<object>(), HttpStatusCode.Created, default(KeyValuePair<string, string>))).Returns(() => ItemBase);

            ProjectMeta = new MetaProject
            {
                IssueTypes = new List<MetaIssueType>
                {
                    new MetaIssueType
                    {
                        Name = "Epic", Fields = new MetaField { 
                            Properties = new List<MetaProperty>
                            {
                                new MetaProperty { Key = "customfield_10006", Property = "Epic Name", Schema = "com.pyxis.greenhopper.jira:gh" }
                            }
                        }
                    }
                }
            };

            Jira = new JiraWorker.Domain.Jira(MockJiraConnector.Object, ProjectMeta, null);
        }
    }

    [TestClass]
    public class new_epic_has_no_priority_set : When_trying_to_post_a_new_epic_on_jira_without_priority
    {
        [TestInitialize]
        public void Context()
        {
            BuildPriorityContext();

            Epic.Priority.ShouldBeNull();

            Jira.CreateEpic(Epic, JiraKey);
        }

        [TestMethod]
        public void null_v1_priority_returns_empty_string()
        {
            MockJiraConnector.Verify(
                x =>
                    x.Post<ItemBase>(It.IsAny<string>(),
                        It.Is<ExpandoObject>(
                            arg =>
                                !((Dictionary<string, object>)((IDictionary<string, object>)arg)["fields"])
                                    .ContainsKey("priority")), HttpStatusCode.Created,
                        default(KeyValuePair<string, string>)));
        }

        [TestMethod]
        public void do_not_call_GetJiraPriorityIdFromMapping()
        {
            MockJiraSettings.Verify(x => x.GetJiraPriorityIdFromMapping(It.IsAny<string>(), null), Times.Never);
        }
    }

    [TestClass]
    public class new_epic_has_priority_set : When_trying_to_post_a_new_epic_on_jira_without_priority
    {
        [TestInitialize]
        public void Context()
        {
            BuildPriorityContext();

            Epic.Priority = "Medium";

            Jira.CreateEpic(Epic, JiraKey);
        }

        [TestMethod]
        public void null_v1_priority_returns_empty_string()
        {
            MockJiraConnector.Verify(
                x =>
                    x.Post<ItemBase>(It.IsAny<string>(),
                        It.Is<ExpandoObject>(
                            arg =>
                                ((Dictionary<string, object>)((IDictionary<string, object>)arg)["fields"])
                                    .ContainsKey("priority")), HttpStatusCode.Created,
                        default(KeyValuePair<string, string>)));
        }

        [TestMethod]
        public void do_not_call_GetJiraPriorityIdFromMapping()
        {
            MockJiraSettings.Verify(x => x.GetJiraPriorityIdFromMapping(It.IsAny<string>(), "Medium"), Times.Once);
        }
    }

    public abstract class When_trying_to_update_an_epic_on_jira : worker_bits
    {
        protected Epic Epic;
        protected SearchResult SearchResult;
        protected EpicWorker Worker;

        protected virtual void BuildPriorityContext()
        {
            BuildContext();

            SearchResult = new SearchResult();
            SearchResult.issues.Add(new Issue
            {
                Key = "OPC-10",
                Fields = new Fields
                {
                    Status = new Status { Name = "ToDo" },
                    Priority = new Priority
                    {
                        Id = "3",
                        Name = "Medium"
                    }
                }
            });

            MockJira.Setup(x => x.GetEpicsInProject(It.IsAny<string>())).Returns(SearchResult);

            SearchResult.issues[0].Key.ShouldNotBeNull("need a reference");

            Worker = new EpicWorker(MockV1.Object, MockV1Log.Object);
        }
    }

    [TestClass]
    public class modified_epic_has_no_priority_set : When_trying_to_update_an_epic_on_jira
    {
        [TestInitialize]
        public async void Context()
        {
            BuildPriorityContext();

            Epic = new Epic { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10", ScopeName = "v1", AssetState = "64" };

            MockV1.Setup(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                Epic
            });

            Epic.Reference.ShouldNotBeNull("need a reference");
            Epic.IsClosed().ShouldBeFalse();

            await Worker.UpdateEpics(MockJira.Object);
        }

        [TestMethod]
        public void call_GetJiraPriorityIdFromMapping_with_null_priority()
        {
            MockJiraSettings.Verify(x => x.GetJiraPriorityIdFromMapping(It.IsAny<string>(), null), Times.Once);
        }

        [TestMethod]
        public void null_v1_priority_returns_empty_string()
        {
            MockJira.Verify(
                x =>
                    x.UpdateIssue(
                        It.Is<object>(
                            arg => arg.GetHashCode() == (new
                            {
                                fields = new
                                {
                                    description = "descript",
                                    summary = "Johnny",
                                    priority = new { id = (string)null },
                                    labels = SearchResult.issues[0].Fields.Labels
                                }
                            }).GetHashCode()), It.IsAny<string>()));
        }
    }

    [TestClass]
    public class modified_epic_has_priority_set : When_trying_to_update_an_epic_on_jira
    {
        [TestInitialize]
        public async void Context()
        {
            BuildPriorityContext();

            Epic = new Epic { Number = "5", Description = "descript", Name = "Johnny", Reference = "OPC-10", ScopeName = "v1", AssetState = "64", Priority = "Medium" };

            MockV1.Setup(x => x.GetEpicsWithReferenceUpdatedSince(ProjectId, EpicCategory, It.IsAny<DateTime>())).ReturnsAsync(new List<Epic>
            {
                Epic
            });

            Epic.Reference.ShouldNotBeNull("need a reference");
            Epic.IsClosed().ShouldBeFalse();

            await Worker.UpdateEpics(MockJira.Object);
        }

        [TestMethod]
        public void call_GetJiraPriorityIdFromMapping_with_Medium_priority()
        {
            MockJiraSettings.Verify(x => x.GetJiraPriorityIdFromMapping(It.IsAny<string>(), "Medium"), Times.Once);
        }

        [TestMethod]
        public void null_v1_priority_returns_empty_string()
        {
            MockJira.Verify(
                x =>
                    x.UpdateIssue(
                        It.Is<object>(
                            arg => arg.GetHashCode() == (new
                            {
                                fields = new
                                {
                                    description = "descript",
                                    summary = "Johnny",
                                    priority = new { id = "3" },
                                    labels = SearchResult.issues[0].Fields.Labels
                                }
                            }).GetHashCode()), It.IsAny<string>()));
        }
    }

    [TestClass]
    public class new_story_has_priority_set : story_bits
    {
        [TestInitialize]
        public void Context()
        {
            BuildContext();
            NewIssue.Fields.EpicLink = null;

            Worker.CreateStories(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue }, new List<Story> { ExistingStory });
        }

        [TestMethod]
        public void should_sync_member_at_least_once()
        {
            MockJiraSettings.Verify(x => x.GetV1PriorityIdFromMapping(It.IsAny<string>(), "Medium"), Times.Once);
        }
    }

    [TestClass]
    public class modified_story_has_priority_set : story_update
    {
        [TestInitialize]
        public new void Context()
        {
            base.Context();
        }

        [TestMethod]
        public void should_sync_member_at_least_once()
        {
            MockJiraSettings.Verify(x => x.GetV1PriorityIdFromMapping(It.IsAny<string>(), "Medium"), Times.Exactly(2));
        }

        [TestMethod]
        public void should_send_the_right_priority_to_be_updated()
        {
            StorySentToUpdate.Priority.ShouldEqual("WorkitemPriority:139");
        }
    }

    [TestClass]
    public class new_defect_has_priority_set : defect_bits
    {
        [TestInitialize]
        public void Context()
        {
            BuildContext();
            NewIssue.Fields.EpicLink = null;

            Worker.CreateDefects(MockJira.Object, new List<Issue> { ExistingIssue, NewIssue }, new List<Defect> { ExistingDefect });
        }

        [TestMethod]
        public void should_sync_member_at_least_once()
        {
            MockJiraSettings.Verify(x => x.GetV1PriorityIdFromMapping(It.IsAny<string>(), "Medium"), Times.Once);
        }
    }

    [TestClass]
    public class modified_defect_has_priority_set : defect_update
    {
        [TestInitialize]
        public new void Context()
        {
            base.Context();
        }

        [TestMethod]
        public void should_sync_member_at_least_once()
        {
            MockJiraSettings.Verify(x => x.GetV1PriorityIdFromMapping(It.IsAny<string>(), "Medium"), Times.Exactly(2));
        }

        [TestMethod]
        public void should_send_the_right_priority_to_be_updated()
        {
            DefectSentToUpdate.Priority.ShouldEqual("WorkitemPriority:139");
        }
    }
}
