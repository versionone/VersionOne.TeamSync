using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.VersionOne.Domain;

namespace VersionOne.TeamSync.Core.Tests.Xml
{
    [TestClass]
    public class Defect_Tests
    {
        private Defect _defect;

        [TestInitialize]
        public void Context()
        {
            var item = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Asset href=\"/VersionOne.Web/rest-1.v1/Data/Story/3122\" id=\"Story:3122\">" +
                            "<Attribute name=\"Name\">defect template</Attribute>" +
                            "<Relation name=\"Priority\">" +
                                "<Asset href=\"/VersionOne.Web/rest-1.v1/Data/WorkitemPriority/139\" idref=\"WorkitemPriority:139\" />" +
                            "</Relation>" +
                            "<Relation name=\"Owners\">" +
                            "    <Asset href=\"/VersionOne.Web/rest-1.v1/Data/Member/20\" idref=\"Member:20\" />" +
                            "    <Asset href=\"/VersionOne.Web/rest-1.v1/Data/Member/1000\" idref=\"Member:1000\" />" +
                            "</Relation>" +
                            "<Attribute name=\"Owners.Name\">" +
                                "<Value>Administrator</Value>" +
                                "<Value>Super User</Value>" +
                            "</Attribute>" +
                            "<Attribute name=\"Owners.Nickname\">" +
                                "<Value>admin</Value>" +
                                "<Value>superuser</Value>" +
                            "</Attribute>" +
                        "</Asset>";

            var xml = XDocument.Parse(item);

            _defect = Defect.FromQuery(xml.Root);
        }

        [TestMethod]
        public void defect_should_have_a_name_of_defect_template()
        {
            _defect.Name.ShouldEqual("defect template");
        }

        [TestMethod]
        public void defect_should_have_a_priority_of_139()
        {
            _defect.Priority.ShouldEqual("WorkitemPriority:139");
        }

        [TestMethod]
        public void defect_should_have_two_owners()
        {
            _defect.OwnersIds.Count.ShouldEqual(2);
        }

        [TestMethod]
        public void defects_first_owner_should_be_Member20()
        {
            _defect.OwnersIds.First().ShouldEqual("Member:20");
        }

        [TestMethod]
        public void defects_second_owner_should_be_Member1000()
        {
            _defect.OwnersIds.Last().ShouldEqual("Member:1000");
        }
    }
}
