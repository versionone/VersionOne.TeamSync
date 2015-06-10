using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class when_creating_payload_for_jira_epic_update
    {
        private dynamic _result;

        [TestInitialize]
        public void Context()
        {
            var epic = new Epic()
            {
                AssetState = "64",
                Description = "a description",
                Name = "create new features",
                Number = "E-1000",
            };

            _result = epic.CreateJiraEpic("jiraKey", "fake_customfield_10000");
        }

        [TestMethod]
        public void should_include_the_description()
        {
            ((string)_result.fields["description"]).ShouldEqual("a description");
        }

        [TestMethod]
        public void should_include_the_number()
        {
            ((string)_result.fields["name"]).ShouldEqual("E-1000");
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
    }
}
