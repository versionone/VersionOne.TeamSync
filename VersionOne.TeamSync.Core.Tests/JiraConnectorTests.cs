﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;
using Should;
using VersionOne.TeamSync.JiraConnector;
using VersionOne.TeamSync.JiraConnector.Connector;
using VersionOne.TeamSync.JiraConnector.Exceptions;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Core.Tests
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

    [TestClass]
    public class for_setting_a_project_to_todo
    {
        private const string _issueKey = "AKey-10";
        [TestMethod]
        public void should_request_an_update_correctly()
        {
            var mockConnector = new Mock<IJiraConnector>();
            mockConnector.Setup(x => x.Post("issue/" + _issueKey + "/transitions", It.IsAny<object>(), HttpStatusCode.NoContent, default(KeyValuePair<string, string>))).Verifiable();

            var jira = new Jira(mockConnector.Object);

            jira.SetIssueToToDo(_issueKey);

            mockConnector.VerifyAll();
        }
    }

    [TestClass]
    public class getting_epic_by_key
    {
        private List<JqOperator> _jqOperators;
        private List<string> _whereItems;
        private const string _issueKey = "AKey-10";

        [TestInitialize]
        public void when_doing_an_epic_search()
        {
            var mockConnector = new Mock<IJiraConnector>();

            _jqOperators = new List<JqOperator>();
            _whereItems = new List<string>();
            mockConnector.Setup(x => x.GetSearchResults(It.IsAny<IList<JqOperator>>(), It.IsAny<IEnumerable<string>>()))
                .Callback<IList<JqOperator>, IEnumerable<string>>((list, enumerable) =>
                {
                    _jqOperators.AddRange(list);
                    _whereItems.AddRange(enumerable);
                });

            var jira = new Jira(mockConnector.Object);

            jira.GetEpicByKey(_issueKey);
        }

        [TestMethod]
        public void should_have_two_jqOperators()
        {
            _jqOperators.Count.ShouldEqual(2);
        }

        [TestMethod]
        public void should_have_one_segment_for_issue_key()
        {
            var op = _jqOperators.Single(x => x.Value == _issueKey);
            op.Property.ShouldEqual("key");
        }

        [TestMethod]
        public void should_have_one_segment_for_issue_type()
        {
            var op = _jqOperators.Single(x => x.Value == "Epic");
            op.Property.ShouldEqual("issuetype");
        }

        [TestMethod]
        public void should_have_seven_query_items()
        {
            _whereItems.Count.ShouldEqual(7);
            _whereItems.Contains("issuetype").ShouldBeTrue();
            _whereItems.Contains("summary").ShouldBeTrue();
            _whereItems.Contains("timeoriginalestimate").ShouldBeTrue();
            _whereItems.Contains("description").ShouldBeTrue();
            _whereItems.Contains("status").ShouldBeTrue();
            _whereItems.Contains("key").ShouldBeTrue();
            _whereItems.Contains("self").ShouldBeTrue();
        }
    }

    [TestClass]
    public class for_setting_a_project_to_resolved
    {
        private const string _issueKey = "AKey-10";
        [TestMethod]
        public void should_request_an_update_correctly()
        {
            var mockConnector = new Mock<IJiraConnector>();
            mockConnector.Setup(x => x.Post("issue/" + _issueKey + "/transitions", It.IsAny<object>(), HttpStatusCode.NoContent, default(KeyValuePair<string, string>))).Verifiable();

            var jira = new Jira(mockConnector.Object);

            jira.SetIssueToResolved(_issueKey);

            mockConnector.VerifyAll();
        }
    }

    [TestClass]
    public class Executing_a_request_with_no_return
    {
        private Mock<IRestRequest> _restRequest;
        private Mock<IRestResponse> _restResponse;

        private JiraConnector.Connector.JiraConnector createConnect(HttpStatusCode toReturn)
        {
            var restClient = new Mock<IRestClient>();
            _restRequest = new Mock<IRestRequest>();
            _restResponse = new Mock<IRestResponse>();

            _restResponse.Setup(x => x.StatusCode).Returns(toReturn);
            _restResponse.Setup(x => x.Content).Returns("{}");

            restClient.Setup(x => x.BaseUrl).Returns(new Uri("http://baseUrl"));
            restClient.Setup(x => x.Execute(_restRequest.Object)).Returns(_restResponse.Object);

            return new JiraConnector.Connector.JiraConnector(restClient.Object);

        }

        [TestMethod]
        public void when_the_content_types_match_it_should_do_nothing()
        {
            var connector = createConnect(HttpStatusCode.NoContent);
            connector.Execute(_restRequest.Object, HttpStatusCode.NoContent);
        }

        [TestMethod]
        [ExpectedException(typeof(JiraLoginException))]
        public void when_the_content_type_is_not_authorized_should_throw_a_jira_exception()
        {
            var connector = createConnect(HttpStatusCode.Unauthorized);
            connector.Execute(_restRequest.Object, HttpStatusCode.NoContent);
        }

        [TestMethod]
        public void when_content_does_not_match_and_the_error_is_not_on_the_appropriate_screen_throw_a_jira_exception_with_that_data()
        {
            var connector = createConnect(HttpStatusCode.BadRequest);

            _restResponse.Setup(response => response.Content).Returns(appropriateScreenJsonError);

            try
            {
                connector.Execute(_restRequest.Object, HttpStatusCode.NoContent);
            }
            catch (JiraException jira)
            {
                jira.Message.ShouldEqual("Please expose the field targetProperty on the screen");
                jira.InnerException.ShouldNotBeNull();
            }
        }

        [TestMethod]
        public void when_content_does_not_match_throw_a_jira_exception_with_that_data()
        {
            var connector = createConnect(HttpStatusCode.BadRequest);

            var errorContent = "{\"errorMessages\":[],\"errors\":{\"someError\":\"some other error\"}}";
            _restResponse.Setup(response => response.Content).Returns(errorContent);
            _restResponse.Setup(response => response.StatusDescription).Returns("Errors");
            try
            {
                connector.Execute(_restRequest.Object, HttpStatusCode.NoContent);
            }
            catch (JiraException jira)
            {
                jira.Message.ShouldEqual("Errors");
                jira.InnerException.ShouldNotBeNull();
                jira.InnerException.Message.ShouldEqual(errorContent);
            }
        }

        private const string appropriateScreenJsonError = "{\"errorMessages\":[],\"errors\":{\"targetProperty\":\"Field 'components' cannot be set. It is not on the appropriate screen, or unknown.\"}}";
    }

    [TestClass]
    public class Executing_a_request_with_T_return
    {
        private Mock<IRestRequest> _restRequest;
        private Mock<IRestResponse> _restResponse;

        private JiraConnector.Connector.JiraConnector createConnect(HttpStatusCode toReturn)
        {
            var restClient = new Mock<IRestClient>();
            _restRequest = new Mock<IRestRequest>();
            _restResponse = new Mock<IRestResponse>();

            _restResponse.Setup(x => x.StatusCode).Returns(toReturn);
            _restResponse.Setup(x => x.Content).Returns("{ empty }");

            restClient.Setup(x => x.BaseUrl).Returns(new Uri("http://baseUrl"));
            restClient.Setup(x => x.Execute(_restRequest.Object)).Returns(_restResponse.Object);

            return new JiraConnector.Connector.JiraConnector(restClient.Object);
        }

        [TestMethod]
        public void runs_func_when_things_line_up()
        {
            var connect = createConnect(HttpStatusCode.OK);
            var result = connect.ExecuteWithReturn(_restRequest.Object, HttpStatusCode.OK, s => s);
            result.ShouldEqual("{ empty }");
        }

        [TestMethod]
        public void when_content_type_is_a_bad_request_should_return_the_func()
        {
            var connect = createConnect(HttpStatusCode.BadRequest);
            var result = connect.ExecuteWithReturn(_restRequest.Object, HttpStatusCode.OK, s => s);
            result.ShouldEqual("{ empty }");
        }

        [TestMethod]
        [ExpectedException(typeof(JiraLoginException))]
        public void when_the_content_type_is_not_authorized_should_throw_a_jira_exception()
        {
            var connector = createConnect(HttpStatusCode.Unauthorized);
            connector.ExecuteWithReturn(_restRequest.Object, HttpStatusCode.NoContent, s => s);
        }

        [TestMethod]
        public void when_content_does_not_match_throw_a_jira_exception_with_that_data()
        {
            var connector = createConnect(HttpStatusCode.Ambiguous);

            var errorContent = "{\"errorMessages\":[],\"errors\":{\"someError\":\"some other error\"}}";
            _restResponse.Setup(response => response.Content).Returns(errorContent);
            _restResponse.Setup(response => response.StatusDescription).Returns("Errors");
            try
            {
                connector.ExecuteWithReturn(_restRequest.Object, HttpStatusCode.NoContent, s => s);
            }
            catch (JiraException jira)
            {
                jira.Message.ShouldEqual("Errors");
                jira.InnerException.ShouldNotBeNull();
                jira.InnerException.Message.ShouldEqual(errorContent);
            }
        }



    }
}