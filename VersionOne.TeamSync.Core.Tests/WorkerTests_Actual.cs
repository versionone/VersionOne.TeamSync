using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    public abstract class actual_bits : worker_bits
    {
        protected Worklog Worklog;
        protected Actual Actual;

        protected string WorkItemId = "Defect:1077";

        protected override void BuildContext()
        {
            base.BuildContext();

            Worklog = new Worklog
            {
                id = 10127,
                timeSpentSeconds = 1800,
                started = DateTime.UtcNow
            };

            Actual = new Actual { ID = "1080" };
        }
    }

    [TestClass]
    public class create_actual : actual_bits
    {
        private const string V1Number = "D-01008";
        private const string IssueKey = "STP-2";

        [TestInitialize]
        public void Context()
        {
            BuildContext();

            _mockV1.Setup(x => x.CreateActual(It.IsAny<Actual>())).ReturnsAsync(Actual);

            var worker = new ActualsWorker(_mockV1.Object, _mockLogger.Object);
            worker.CreateActualsFromWorklogs(_mockJira.Object, new List<Worklog> { Worklog }, WorkItemId, V1Number, IssueKey);
        }

        [TestMethod]
        public void should_call_create_actual_just_one_time()
        {
            _mockV1.Verify(x => x.CreateActual(It.IsAny<Actual>()), Times.Once);
        }

        [TestMethod]
        public void makes_a_call_add_created_as_VersionOne_actual_comment()
        {
            _mockJira.Verify(x => x.AddComment(IssueKey, It.IsAny<string>()), Times.Once());
        }
    }

    [TestClass]
    public class actual_update : actual_bits
    {
        private XDocument _updatedActual;

        [TestInitialize]
        public void Context()
        {
            BuildContext();

            Actual.Reference = Worklog.id.ToString();
            Worklog.timeSpentSeconds = 3600;

            _updatedActual = XDocument.Parse(
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Asset href=""/VersionOne/rest-1.v1/Data/Actual/1080/1260"" id=""Actual:1080:1260"">
    <Attribute name=""Date"">2015-07-10T15:42:00.000</Attribute>
    <Attribute name=""Value"">1</Attribute>
    <Attribute name=""Reference"">10127</Attribute>
    <Relation name=""Member"">
        <Asset href=""/VersionOne/rest-1.v1/Data/Member/20"" idref=""Member:20"" />
    </Relation>
    <Relation name=""Scope"">
        <Asset href=""/VersionOne/rest-1.v1/Data/Scope/1003"" idref=""Scope:1003"" />
    </Relation>
    <Relation name=""Workitem"">
        <Asset href=""/VersionOne/rest-1.v1/Data/Defect/1077"" idref=""Defect:1077""/>
    </Relation>
</Asset>");
            _mockV1.Setup(x => x.UpdateAsset(It.IsAny<Actual>(), It.IsAny<XDocument>())).ReturnsAsync(_updatedActual);

            var worker = new ActualsWorker(_mockV1.Object, _mockLogger.Object);
            worker.UpdateActualsFromWorklogs(_mockJira.Object, new List<Worklog> { Worklog }, WorkItemId, new List<Actual> { Actual });
        }

        [TestMethod]
        public void should_call_update_asset_just_one_time()
        {
            _mockV1.Verify(x => x.UpdateAsset(It.IsAny<Actual>(), It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_match_updated_value()
        {
            Convert.ToDouble(_updatedActual.Descendants()
                .Single(e => e.Attributes().Any(a => a.Value.Equals("Value")))
                .Value).ShouldEqual(Worklog.timeSpentSeconds / 3600.0);
        }
    }

    [TestClass]
    public class actual_delete : actual_bits
    {
        private XDocument _deletedActual;

        [TestInitialize]
        public void Context()
        {
            BuildContext();

            _deletedActual = XDocument.Parse(
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Asset href=""/VersionOne/rest-1.v1/Data/Actual/1080/1264"" id=""Actual:1080:1264"">
    <Attribute name=""Date"">2015-07-10T15:42:00.000</Attribute>
    <Attribute name=""Value"">0</Attribute>
    <Attribute name=""Reference"">10127</Attribute>
</Asset>");

            _mockV1.Setup(x => x.UpdateAsset(It.IsAny<Actual>(), It.IsAny<XDocument>()));

            var worker = new ActualsWorker(_mockV1.Object, _mockLogger.Object);
            worker.DeleteActualsFromWorklogs(new List<Actual> { Actual });
        }

        [TestMethod]
        public void should_call_update_asset_just_one_time()
        {
            _mockV1.Verify(x => x.UpdateAsset(It.IsAny<Actual>(), It.IsAny<XDocument>()), Times.Once);
        }

        [TestMethod]
        public void should_update_value_to_zero()
        {
            _deletedActual.Descendants().Single(e => e.Attributes().Any(a => a.Value.Equals("Value"))).Value.ShouldEqual("0");
        }
    }
}