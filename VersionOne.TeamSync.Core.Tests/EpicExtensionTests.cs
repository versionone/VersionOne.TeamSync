using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.JiraWorker.Extensions;
using VersionOne.TeamSync.VersionOneWorker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class when_creating_payload_for_jira_epic_update
    {
        private dynamic _result;

        [TestInitialize]
        public void Context()
        {
            var epic = new Epic
            {
                AssetState = "64",
                Description = "a description",
                Name = "create new features",
                Number = "E-1000",
            };

            _result = epic.CreateJiraEpic("jiraKey", "fake_customfield_10000", "20000");
        }

        [TestMethod]
        public void should_include_the_description()
        {
            ((string)_result.fields["description"]).ShouldEqual("a description");
        }

        [TestMethod]
        public void should_include_the_summary()
        {
            ((string)_result.fields["summary"]).ShouldEqual("create new features");
        }

        [TestMethod]
        public void should_include_the_custom_field_for_epic_name()
        {
            ((string)_result.fields["fake_customfield_10000"]).ShouldEqual("create new features");
        }

        [Ignore]
        [TestMethod] //not sure how to cast this correctly
        public void should_include_issue_type()
        {
            ((Dictionary<string, object>)_result.fields["issuetype"])["name"].ShouldEqual("Epic");
        }

        [TestMethod]
        public void should_include_labels()
        {
            ((string)_result.fields["labels"][0]).ShouldEqual("E-1000");
        }
    }

    [TestClass]
    public class when_comparing_epic_to_issue
    {
        private Epic _epic;
        private Issue _issue;

        [TestInitialize]
        public void Context()
        {
            _epic = new Epic
            {
                Description = "Why is the ogre all angry?",
                Name = "Odysseus",
                Reference = "Prince Telemachus"
            };

            _issue = new Issue
            {
                Key = "Prince Telemachus",
                Fields = new Fields
                {
                    Description = "Why is the ogre all angry?",
                    Summary = "Odysseus"
                }
            };
        }

        [TestMethod]
        public void sanity_check()
        {
            _epic.ItMatches(_issue).ShouldBeTrue("if this breaks, the data might be messed up");
        }

        [TestMethod]
        public void description_is_null_or_empty()
        {
            _issue.Fields.Description = null;
            _epic.ItMatches(_issue).ShouldBeFalse();

            _issue.Fields.Description = string.Empty;
            _epic.ItMatches(_issue).ShouldBeFalse();
        }

        [TestMethod]
        public void summary_is_different()
        {
            _issue.Fields.Summary = "Ithica";
            _epic.ItMatches(_issue).ShouldBeFalse();
        }

        [TestMethod]
        public void reference_is_null_or_empty()
        {
            _issue.Key = "wat?!";
            _epic.ItMatches(_issue).ShouldBeFalse();
        }
    }
}
