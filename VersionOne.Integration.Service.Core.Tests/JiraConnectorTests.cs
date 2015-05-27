using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using Should;

namespace VersionOne.Integration.Service.Worker.Tests
{
    [TestClass]
    public class when_seaching_in_one_project_for_items_in_jira : when_using_search
    {
        [TestInitialize]
        public void Context()
        {
            _query = new Dictionary<string, IEnumerable<string>>
            {
                {"stuff", new [] {"AS"}}
            };
            MakeRequest();
        }

        [TestMethod]
        public void query_quote_reserved_words()
        {
            _resultRequest.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("stuff=\"AS\"");
        }
    }

    [TestClass]
    public class and_the_project_is_not_a_reserved_word : when_using_search
    {
        [TestInitialize]
        public void Context()
        {
            _query = new Dictionary<string, IEnumerable<string>>
            {
                {"stuff", new [] {"FERRARI"}}
            };
            MakeRequest();
        }

        [TestMethod]
        public void query_should_leave_the_query_string_alone()
        {
            _resultRequest.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("stuff=FERRARI");
        }
    }

    [TestClass]
    public class for_many_projects_for_items_in_jira : when_using_search
    {
        [TestInitialize]
        public void Context()
        {
            _query = new Dictionary<string, IEnumerable<string>>
            {
                {"stuff", new [] {"AS", "NE", "MA"}}
            };
            MakeRequest();
        }

    
        [TestMethod]
        public void query_should_leave_query_words_alone()
        {
            _resultRequest.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("stuff in (AS, NE, MA)");
        }
    }

    public abstract class when_using_search
    {
        protected RestRequest _resultRequest;
        protected Dictionary<string, IEnumerable<string>> _query;


        public void MakeRequest()
        {
            _resultRequest = JiraConnector.Connector.JiraConnector.BuildSearchRequest(_query, new[] { "item", "item2", "item3" });
        }

        [TestMethod]
        public void has_two_parameters()
        {
            _resultRequest.Parameters.Count.ShouldEqual(2);
        }

    }

}
