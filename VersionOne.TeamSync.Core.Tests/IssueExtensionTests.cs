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

            _story = issue.ToV1Story("Scope:1000");

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
}
