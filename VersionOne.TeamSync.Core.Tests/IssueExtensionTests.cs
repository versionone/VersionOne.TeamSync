using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class IssueExtensionTests
    {
        private Story _story;
        private Actual _actual;
        private DateTime _now;

        [TestInitialize]
        public void Context()
        {
            _now = DateTime.UtcNow;

            var issue = new Issue
            {
                Key = "OPC-10",
                Fields = new Fields
                {
                    Summary = "issue summary",
                    StoryPoints = "17",
                    TimeTracking = new TimeTracking
                    {
                        RemainingEstimateSeconds = 14400,
                        TimeSpentSeconds = 28800
                    }
                },
                RenderedFields = new RenderedFields
                {
                    Description = "issue description"
                }
            };

            _story = issue.ToV1Story("Scope:1000", "");

            var worklog = new Worklog
            {
                started = _now,
                timeSpentSeconds = 1800,
                id = 10127
            };

            _actual = worklog.ToV1Actual("Member: 20", "Scope:1003", "Defect:1064");
        }

        [TestMethod]
        public void issue_summary_becomes_name()
        {
            _story.Name.ShouldEqual("issue summary");
        }

        [TestMethod]
        public void issue_description_becomes_description()
        {
            _story.Description.ShouldEqual("issue description");
        }

        [TestMethod]
        public void issue_story_points_becomes_estimates()
        {
            _story.Estimate.ShouldEqual("17");
        }

        [TestMethod]
        public void issue_time_tracking_becomes_ToDo()
        {
            _story.ToDo.ShouldEqual("4");
        }

        [TestMethod]
        public void issue_key_becomes_reference()
        {
            _story.Reference.ShouldEqual("OPC-10");
        }

        [TestMethod]
        public void passed_in_scope_becomes_story_scopeId()
        {
            _story.ScopeId.ShouldEqual("Scope:1000");
        }

        [TestMethod]
        public void worklog_started_becomes_date()
        {
            _actual.Date.ShouldEqual(_now);
        }

        [TestMethod]
        public void worklog_timeSpentSeconds_becomes_value()
        {
            _actual.Value.ShouldEqual("0.5");
        }

        [TestMethod]
        public void worklog_id_becomes_reference()
        {
            _actual.Reference.ShouldEqual("10127");
        }

        [TestMethod]
        public void passed_in_member_becomes_actual_memberId()
        {
            _actual.MemberId.ShouldEqual("Member: 20");
        }

        [TestMethod]
        public void passed_in_scope_becomes_actual_scopeId()
        {
            _actual.ScopeId.ShouldEqual("Scope:1003");
        }

        [TestMethod]
        public void passed_in_workitem_becomes_actual_workItemId()
        {
            _actual.WorkItemId.ShouldEqual("Defect:1064");
        }
    }

    [TestClass]
    public class IssueExtension_MatchesTests
    {
        private string _reference = "PROJ-10";
        private string _description = "a description";
        private string _summary = "a summary";
        private string _epicLink = "E-1000";
        private int _days = 10;
        private string _points = "5";

        private Issue CreateIssue()
        {
            return new Issue()
            {
                Key = _reference,
                RenderedFields = new RenderedFields()
                {
                    Description = _description
                },
                Fields = new Fields()
                {
                    Summary = _summary,
                    StoryPoints = _points,
                    TimeTracking = new TimeTracking() {RemainingEstimateSeconds = 3600*10},
                    EpicLink = _epicLink,
                }
            };
        }

        private Story CreateStory()
        {
            return new Story()
            {
                Name = _summary,
                Description = _description,
                Estimate = _points,
                ToDo = _days.ToString(),
                Reference = _reference,
                SuperNumber = _epicLink,
            };
        }

        private Defect CreateDefect()
        {
            return new Defect()
            {
                Name = _summary,
                Description = _description,
                Estimate = _points,
                ToDo = _days.ToString(),
                Reference = _reference,
                SuperNumber = _epicLink,
            };
        }

        private void DoesNotMatch(Issue issue)
        {
            issue.ItMatchesStory(CreateStory()).ShouldBeFalse();
            issue.ItMatchesDefect(CreateDefect()).ShouldBeFalse();
        }

        [TestMethod]
        public void should_match_story()
        {
            CreateIssue().ItMatchesStory(CreateStory()).ShouldBeTrue();
        }

        [TestMethod]
        public void should_match_defect()
        {
            CreateIssue().ItMatchesDefect(CreateDefect()).ShouldBeTrue();
        }

        [TestMethod]
        public void should_not_match_if_summary_is_different()
        {
            var issue = CreateIssue();
            issue.Fields.Summary = "something else";
            DoesNotMatch(issue);
        }

        [TestMethod]
        public void should_not_match_if_description_is_different()
        {
            var issue = CreateIssue();
            issue.RenderedFields.Description = "something else";
            DoesNotMatch(issue);
        }

        [TestMethod]
        public void should_not_match_if_story_points_is_different()
        {
            var issue = CreateIssue();
            issue.Fields.StoryPoints= "9";
            DoesNotMatch(issue);
        }

        [TestMethod]
        public void should_not_match_if_remainingInDays_is_different()
        {
            var issue = CreateIssue();
            issue.Fields.TimeTracking.RemainingEstimateSeconds = 3600 * 20;
            DoesNotMatch(issue);
        }

        [TestMethod]
        public void should_not_match_if_the_key_is_different()
        {
            var issue = CreateIssue();
            issue.Key = "OCT-20";
            DoesNotMatch(issue);
        }

        [TestMethod]
        public void should_not_match_if_epicLink_is_different()
        {
            var issue = CreateIssue();
            issue.Fields.EpicLink = "E-3045";
            DoesNotMatch(issue);
        }

        [TestMethod]
        public void should_be_a_match_even_if_someone_adds_spaces()
        {
            var issue = CreateIssue();
            issue.Fields.Summary += " ";
            issue.ItMatchesStory(CreateStory()).ShouldBeTrue();
            issue.ItMatchesDefect(CreateDefect()).ShouldBeTrue();
        }
    }
}
