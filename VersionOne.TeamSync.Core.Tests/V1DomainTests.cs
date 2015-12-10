using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.TeamSync.JiraWorker.Domain;
using VersionOne.TeamSync.V1Connector.Interfaces;
using VersionOne.TeamSync.VersionOneWorker.Domain;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class V1DomainTests
    {
        private IV1 SetApiQuery(string type, Mock<IV1Connector> mockConnect, string[] properties, string[] whereClauses, List<Epic> epics)
        {
            mockConnect.Setup(x => x.Query(type, properties, whereClauses, Epic.FromQuery))
                .ReturnsAsync(epics)
                .Verifiable("Query has been modified or incorrect");

            return new V1(mockConnect.Object);
        }

        [TestMethod]
        public async Task make_sure_its_getting_refereces_correctly()
        {
            var mockConnector = new Mock<IV1Connector>();

            var api = SetApiQuery("Epic", mockConnector,
                new[] { "ID.Number", "Name", "Description", "Scope.Name", "Priority.Name", "Status.Name" },
                new[] { "Reference=\"\"", "AssetState='Active'", "Scope=\"Scope:1000\"", "Category=\"EpicCategory:1000\"" },
                new List<Epic>());

            await api.GetEpicsWithoutReference("Scope:1000", "EpicCategory:1000");

            mockConnector.VerifyAll();
        }

        [TestMethod]
        public async Task closed_tracked_epics_should_grab_just_the_name_assetState_and_reference()
        {
            var mockConnector = new Mock<IV1Connector>();

            var api = SetApiQuery("Epic", mockConnector,
                new[] { "Name", "AssetState", "Reference" },
                new[]
                {
                    "Reference!=\"\"", "AssetState='Closed'", "Scope=\"Scope:1000\"", "Category=\"EpicCategory:1000\"",
                    "ChangeDateUTC>='01/01/0001 00:00:00'"
                },
                new List<Epic>());

            await api.GetClosedTrackedEpicsUpdatedSince("Scope:1000", "EpicCategory:1000", DateTime.MinValue);

            mockConnector.VerifyAll();
        }

        [TestMethod]
        public async Task getting_tracked_epics_is_number_name_and_ref()
        {
            var mockConnector = new Mock<IV1Connector>();

            var api = SetApiQuery("Epic", mockConnector,
                new[] { "ID.Number", "Name", "Description", "Reference", "AssetState", "Priority.Name", "Status.Name" },
                new[]
                {
                    "Reference!=\"\"", "Scope=\"Scope:1000\"", "Category=\"EpicCategory:1000\"",
                    "ChangeDateUTC>='01/01/0001 00:00:00'"
                },
                new List<Epic>());

            await api.GetEpicsWithReferenceUpdatedSince("Scope:1000", "EpicCategory:1000", DateTime.MinValue);

            mockConnector.VerifyAll();
        }

        [TestMethod]
        public async Task deleted_epics_need_a_query_for_isDeleted_along_with_basic_attributes()
        {
            var mockConnector = new Mock<IV1Connector>();

            var api = SetApiQuery("Epic", mockConnector,
                new[] { "ID.Number", "Name", "Description", "Reference" },
                new[]
                {
                    "Reference!=\"\"", "IsDeleted='True'", "Scope=\"Scope:1000\"", "Category=\"EpicCategory:1000\"",
                    "ChangeDateUTC>='01/01/0001 00:00:00'"
                },
                new List<Epic>());

            await api.GetDeletedEpicsUpdatedSince("Scope:1000", "EpicCategory:1000", DateTime.MinValue);

            mockConnector.VerifyAll();
        }

        [TestMethod]
        public async Task get_all_the_stories_in_a_project_with_a_reference()
        {
            var mockConnector = new Mock<IV1Connector>();
            mockConnector.Setup(x => x.Query("Story",
                new[]
                {
                    "ID.Number", "Name", "Description", "Estimate", "ToDo", "Reference", "IsInactive", "AssetState",
                    "Super.Number", "Priority", "Owners", "Status"
                },
                new[] { "Reference!=\"\"", "Scope=\"Scope:1000\"" },
                Story.FromQuery))
                .ReturnsAsync(new List<Story>());
            var api = new V1(mockConnector.Object);

            await api.GetStoriesWithJiraReference("Scope:1000");

            mockConnector.VerifyAll();
        }
    }
}