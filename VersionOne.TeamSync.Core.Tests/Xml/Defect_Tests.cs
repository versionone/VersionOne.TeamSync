using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests.Xml
{
    [TestClass]
    public class Defect_Tests
    {
        [TestInitialize]
        public void Context()
        {
            
        }

        [TestMethod]
        public void Defect_FromQuery_Tests()
        {
            var item = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Asset href=\"/VersionOne.Web/rest-1.v1/Data/Story/3122\" id=\"Story:3122\">" +
	                        "<Attribute name=\"Name\">story template</Attribute>" +
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

            var defect = Defect.FromQuery(xml.Root);
        }
    }
}
