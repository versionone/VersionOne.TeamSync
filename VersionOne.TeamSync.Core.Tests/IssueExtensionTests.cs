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

        [TestInitialize]
        public void Context()
        {
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
        public void passed_in_scope_becomes_scopeId()
        {
            _story.ScopeId.ShouldEqual("Scope:1000");
        }
    }

}
