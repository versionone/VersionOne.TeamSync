﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VersionOne.Integration.Service.Worker.Domain;
using VersionOne.Integration.Service.Worker.Extensions;
using VersionOne.SDK.APIClient;

namespace VersionOne.Integration.Service.Worker.Tests
{
    [TestClass]
    public class V1DomainTests
    {
        private DateTime _timeAgo = new DateTime(2015, 1, 1, 16, 0, 0);
        private readonly TimeSpan _span = new TimeSpan(0, 0, 0, 15);
        private Mock<IDateTime> _mockDateTime;

        [TestInitialize]
        public void Context()
        {
            _mockDateTime = new Mock<IDateTime>();
            _mockDateTime.Setup(x => x.UtcNow).Returns(new DateTime(2015, 1, 1, 16, 0, 0));
        }

        private V1 SetApiQuery(Mock<IV1Connector> mockConnect, string[] properties, string[] whereClauses, List<Epic> epics)
        {
            mockConnect.Setup(x => x.Query("Epic", properties, whereClauses, Epic.FromQuery))
                         .ReturnsAsync(epics)
                         .Verifiable("Query has been modified or incorrect");

            return new V1(mockConnect.Object, _mockDateTime.Object, _span);
        }

        [TestMethod]
        public async Task make_sure_its_getting_refereces_correctly()
        {
            var mockConnector = new Mock<IV1Connector>();

            var api = SetApiQuery(mockConnector, new[] { "ID.Number", "Name", "Description", "Scope.Name" }, 
                             new[] { "Reference=\"\"", "AssetState='Active'", "CreateDateUTC>=" + _timeAgo.Add(-_span).ToString("yyyy-MM-dd HH:mm:ss").InQuotes() },
                             new List<Epic>());

            await api.GetEpicsWithoutReference();

            mockConnector.VerifyAll();
        }


        [TestMethod]
        public async Task closed_tracked_epics_should_grab_just_the_name_assetState_and_reference()
        {
            var mockConnector = new Mock<IV1Connector>();

            var api = SetApiQuery(mockConnector, 
                new[] { "Name", "AssetState", "Reference" }, 
                new[] { "Reference!=\"\"", "AssetState='Closed'", "ChangeDateUTC>=" + _timeAgo.Add(-_span).ToString("yyyy-MM-dd HH:mm:ss").InQuotes() },
                             new List<Epic>());

            await api.GetClosedTrackedEpics();

            mockConnector.VerifyAll();
        }
        
        [TestMethod]
        public async Task getting_tracked_epics_is_number_name_and_ref()
        {
            var mockConnector = new Mock<IV1Connector>();

            var api = SetApiQuery(mockConnector,
                new[] { "ID.Number", "Name", "Description", "Reference" },
                new[] { "Reference!=\"\"", "ChangeDateUTC>=" + _timeAgo.Add(-_span).ToString("yyyy-MM-dd HH:mm:ss").InQuotes() },
                             new List<Epic>());

            await api.GetEpicsWithReference();

            mockConnector.VerifyAll();
        }

        [TestMethod]
        public async Task deleted_epics_need_a_query_for_isDeleted_along_with_basic_attributes()
        {
            var mockConnector = new Mock<IV1Connector>();

            var api = SetApiQuery(mockConnector,
                new[] { "ID.Number", "Name", "Description", "Reference" },
                new[] { "Reference!=\"\"", "IsDeleted='True'", "ChangeDateUTC>=" + _timeAgo.Add(-_span).ToString("yyyy-MM-dd HH:mm:ss").InQuotes() },
                             new List<Epic>());

            await api.GetDeletedEpics();

            mockConnector.VerifyAll();
        }
    }
}