using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraConnector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class and_epic_has_no_priority_set : worker_bits
    {
        private Mock<IJiraConnector> _mockJiraConnector;

        [TestInitialize]
        public void Context()
        {
            BuildContext();

            var epic = new Epic { Number = "5", Description = "descript", Name = "Johnny", ScopeName = "v1" };
            var itemBase = new ItemBase { Key = JiraKey };

            _mockJiraConnector = new Mock<IJiraConnector>();
            _mockJiraConnector.Setup(
                x => x.Post<ItemBase>(It.IsAny<string>(), It.IsAny<object>(), HttpStatusCode.Created, default(KeyValuePair<string, string>))).Returns(() => itemBase);

            var projectMeta = new MetaProject
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

            epic.Priority.ShouldBeNull();

            var jira = new Jira(_mockJiraConnector.Object, projectMeta, null);
            jira.CreateEpic(epic, JiraKey);
        }

        [TestMethod]
        public void null_v1_priority_returns_empty_string()
        {
            _mockJiraConnector.Verify(x => x.Post<ItemBase>(It.IsAny<string>(), It.Is<ExpandoObject>(arg => !((Dictionary<string, object>)((IDictionary<string, object>)arg)["fields"]).ContainsKey("Priority")), HttpStatusCode.Created, default(KeyValuePair<string, string>)));
        }

        [TestMethod]
        public void do_not_call_GetJiraPriorityIdFromMapping()
        {
            MockJiraSettings.Verify(x => x.GetJiraPriorityIdFromMapping(It.IsAny<string>(), null), Times.Never);
        }
    }
}
