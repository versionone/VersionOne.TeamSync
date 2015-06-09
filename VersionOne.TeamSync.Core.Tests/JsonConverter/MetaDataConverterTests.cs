using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Should;
using VersionOne.TeamSync.JiraConnector.Connector;
using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.Core.Tests.JsonConverter
{
    [TestClass]
    public class MetaDataConverterTests
    {
        private CreateMeta _result;

        [TestMethod]
        public void should_have_one_project()
        {
            _result.Projects.Count.ShouldEqual(1);
        }

        [TestMethod]
        public void should_have_5_sets_of_issues()
        {
            _result.Projects.Single().IssueTypes.Count.ShouldEqual(5);
        }

        [TestMethod]
        public void epic_type_should_have_15_properties()
        {
            _result.Projects.Single()
                .Epic
                .Fields
                .Properties.Count.ShouldEqual(15);
        }

        [TestMethod]
        public void epic_type_should_have_3_official_custom_fields()
        {
            _result.Projects.Single()
                .OfficialEpicCustomFields
                .Count.ShouldEqual(3);
        }

        [TestMethod]
        public void epic_name_should_have_the_property_key()
        {
            _result.Projects.Single().EpicName.Key.ShouldEqual("customfield_10006");
        }

        [TestInitialize]
        public void context()
        {
            var jsonResult = File.ReadAllText("JsonConverter/CreateMeta.json");
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new LowercaseContractResolver(),
            };
            //jsonSerializerSettings.Converters.Add(new MetaData());
            _result = JsonConvert.DeserializeObject<CreateMeta>(jsonResult, jsonSerializerSettings);

        }

    }
}
