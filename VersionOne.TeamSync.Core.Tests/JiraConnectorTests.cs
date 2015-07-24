using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RestSharp;
using Should;
using VersionOne.TeamSync.JiraConnector;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.JiraConnector.Exceptions;
using VersionOne.TeamSync.JiraConnector.Interfaces;
using VersionOne.TeamSync.Worker.Domain;
using VersionOne.TeamSync.Worker.Extensions;

namespace VersionOne.TeamSync.Core.Tests
{
    [TestClass]
    public class when_seaching_in_one_project_for_items_in_jira : when_using_search
    {
        [TestInitialize]
        public void Context()
        {
            Query = new Dictionary<string, IEnumerable<string>>
            {
                {"stuff", new [] {"AS"}}
            };
            MakeRequest();
        }

        [TestMethod]
        public void query_quote_reserved_words()
        {
            ResultRequest.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("stuff=\"AS\"");
        }
    }

    [TestClass]
    public class and_the_project_is_not_a_reserved_word : when_using_search
    {
        [TestInitialize]
        public void Context()
        {
            Query = new Dictionary<string, IEnumerable<string>>
            {
                {"stuff", new [] {"FERRARI"}}
            };
            MakeRequest();
        }

        [TestMethod]
        public void query_should_leave_the_query_string_alone()
        {
            ResultRequest.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("stuff=FERRARI");
        }
    }

    [TestClass]
    public class for_many_projects_for_items_in_jira : when_using_search
    {
        [TestInitialize]
        public void Context()
        {
            Query = new Dictionary<string, IEnumerable<string>>
            {
                {"stuff", new [] {"AS", "NE", "MA"}}
            };
            MakeRequest();
        }

        [TestMethod]
        public void query_should_leave_query_words_alone()
        {
            ResultRequest.Parameters.Single(x => x.Name == "jql").Value.ShouldEqual("stuff in (AS, NE, MA)");
        }
    }

    [TestClass]
    public abstract class when_using_search
    {
        protected RestRequest ResultRequest;
        protected Dictionary<string, IEnumerable<string>> Query;

        public void MakeRequest()
        {
            ResultRequest = JiraConnector.Connector.JiraConnector.BuildSearchRequest(Query, new[] { "item", "item2", "item3" });
        }

        [TestMethod]
        public void has_two_parameters()
        {
            ResultRequest.Parameters.Count.ShouldEqual(2);
        }
    }

    [TestClass]
    public class for_setting_a_project_to_todo
    {
        private const string IssueKey = "AKey-10";

        [TestMethod]
        public void should_request_an_update_correctly()
        {
            var mockConnector = new Mock<IJiraConnector>();
            mockConnector.Setup(x => x.Get<TransitionResponse>(It.IsAny<string>(), It.IsAny<KeyValuePair<string, string>>()))
                .Returns(new TransitionResponse() {Transitions = new List<Transition>()
                {
                    new Transition() {Id = "1",Name = "Done"},
                    new Transition() {Id = "1",Name = "In Progress"}
                } });
            mockConnector.Setup(x => x.Post("issue/{issueIdOrKey}/transitions", It.IsAny<object>(), HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", IssueKey)))
                .Verifiable();

            var jira = new Jira(mockConnector.Object, string.Empty);

            jira.SetIssueToToDo(IssueKey, new [] {"Done"});

            mockConnector.VerifyAll();
        }
    }

    [TestClass]
    public class getting_epic_by_key
    {
        private const string IssueKey = "AKey-10";

        private List<JqOperator> _jqOperators;
        private List<string> _whereItems;

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

            var jira = new Jira(mockConnector.Object, string.Empty);

            jira.GetEpicByKey(IssueKey);
        }

        [TestMethod]
        public void should_have_two_jqOperators()
        {
            _jqOperators.Count.ShouldEqual(2);
        }

        [TestMethod]
        public void should_have_one_segment_for_issue_key()
        {
            var op = _jqOperators.Single(x => x.Value == IssueKey);
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
        private const string IssueKey = "AKey-10";

        [TestMethod]
        public void should_request_an_update_correctly()
        {
            var mockConnector = new Mock<IJiraConnector>();
			mockConnector.Setup(x => x.Get<TransitionResponse>("issue/{issueOrKey}/transitions", new KeyValuePair<string, string>("issueOrKey", IssueKey))).Returns(new TransitionResponse()
			{
				Transitions = new List<Transition>() { new Transition() { Id = "5", Name = "Done"} }
			}).Verifiable();
            mockConnector.Setup(x => x.Post("issue/{issueIdOrKey}/transitions", It.IsAny<object>(), HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", IssueKey))).Verifiable();

            var jira = new Jira(mockConnector.Object, string.Empty);

            jira.SetIssueToResolved(IssueKey, new[] {"Done"});

            mockConnector.VerifyAll();
        }
    }

    [TestClass]
    public class Executing_a_request_with_no_return
    {
        private const string AppropriateScreenJsonError = "{\"errorMessages\":[],\"errors\":{\"targetProperty\":\"Field 'components' cannot be set. It is not on the appropriate screen, or unknown.\"}}";

        private Mock<IRestRequest> _restRequest;
        private Mock<IRestResponse> _restResponse;

        private JiraConnector.Connector.JiraConnector CreateConnect(HttpStatusCode toReturn)
        {
            var restClient = new Mock<IRestClient>();
            _restRequest = new Mock<IRestRequest>();
            _restResponse = new Mock<IRestResponse>();

            _restRequest.Setup(x => x.Method).Returns(Method.GET);
            _restRequest.Setup(x => x.Parameters).Returns(new List<Parameter>());

            _restResponse.Setup(x => x.StatusCode).Returns(toReturn);
            _restResponse.Setup(x => x.Content).Returns("{}");
            _restResponse.Setup(x => x.Headers).Returns(new List<Parameter>());
            restClient.Setup(x => x.BaseUrl).Returns(new Uri("http://baseUrl"));
            restClient.Setup(x => x.Execute(_restRequest.Object)).Returns(_restResponse.Object);

            return new JiraConnector.Connector.JiraConnector(restClient.Object);

        }

        [TestMethod]
        public void when_the_content_types_match_it_should_do_nothing()
        {
            var connector = CreateConnect(HttpStatusCode.NoContent);
            connector.Execute(_restRequest.Object, HttpStatusCode.NoContent);
        }

        [TestMethod]
        [ExpectedException(typeof(JiraLoginException))]
        public void when_the_content_type_is_not_authorized_should_throw_a_jira_exception()
        {
            var connector = CreateConnect(HttpStatusCode.Unauthorized);
            connector.Execute(_restRequest.Object, HttpStatusCode.NoContent);
        }

        [TestMethod]
        public void when_content_does_not_match_and_the_error_is_not_on_the_appropriate_screen_throw_a_jira_exception_with_that_data()
        {
            var connector = CreateConnect(HttpStatusCode.BadRequest);

            _restResponse.Setup(response => response.Content).Returns(AppropriateScreenJsonError);

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
            var connector = CreateConnect(HttpStatusCode.BadRequest);

            const string errorContent = "{\"errorMessages\":[],\"errors\":{\"someError\":\"some other error\"}}";
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
    }

    [TestClass]
    public class Executing_a_request_with_T_return
    {
        private Mock<IRestRequest> _restRequest;
        private Mock<IRestResponse> _restResponse;
        private string _errorMessage;

        private JiraConnector.Connector.JiraConnector CreateConnect(HttpStatusCode toReturn)
        {
            var restClient = new Mock<IRestClient>();
            _restRequest = new Mock<IRestRequest>();
            _restResponse = new Mock<IRestResponse>();

            _restRequest.Setup(x => x.Parameters).Returns(new List<Parameter>());

            _restResponse.Setup(x => x.StatusCode).Returns(toReturn);
            _errorMessage = "{\"errorMessages\":[\"Issue Does Not Exist\"],\"errors\":{}}";
            _restResponse.Setup(x => x.Content).Returns(_errorMessage);
            _restResponse.Setup(x => x.Headers).Returns(new List<Parameter>());
            restClient.Setup(x => x.BaseUrl).Returns(new Uri("http://baseUrl"));
            restClient.Setup(x => x.Execute(_restRequest.Object)).Returns(_restResponse.Object);

            return new JiraConnector.Connector.JiraConnector(restClient.Object);
        }

        [TestMethod]
        public void runs_func_when_things_line_up()
        {
            var connect = CreateConnect(HttpStatusCode.OK);
            var result = connect.Execute(_restRequest.Object, HttpStatusCode.OK);
            result.ShouldEqual(_errorMessage);
        }

        [TestMethod]
        [ExpectedException(typeof(JiraLoginException))]
        public void when_the_content_type_is_not_authorized_should_throw_a_jira_exception()
        {
            var connector = CreateConnect(HttpStatusCode.Unauthorized);
            connector.Execute(_restRequest.Object, HttpStatusCode.NoContent);
        }

        [TestMethod]
        public void when_content_does_not_match_throw_a_jira_exception_with_that_data()
        {
            var connector = CreateConnect(HttpStatusCode.Ambiguous);

            const string errorContent = "{\"errorMessages\":[],\"errors\":{\"someError\":\"some other error\"}}";
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
    }

    [TestClass]
    public class dealing_with_a_property_that_isnt_visible
    {
        private Mock<ILog> _mockLogger;

        [TestInitialize]
        public void getting_jira_stories()
        {
            _mockLogger = new Mock<ILog>();
            var dictionary = new Dictionary<string, string>()
            {
                {"Story Points", "value value"},
                {"Epic Stuff", "something else"}
            };
            var empty = MetaProperty.EmptyProperty("KEY");
            string resultValue;

            dictionary.EvalLateBinding("KEY-10", empty, s => resultValue = s, _mockLogger.Object);
            dictionary.EvalLateBinding("KEY-10", empty, s => resultValue = s, _mockLogger.Object);
        }

        [TestMethod]
        public void should_call_logger_only_one_time()
        {
            _mockLogger.Verify(x => x.Warn(It.IsAny<string>()), Times.Once);
        }
    }

    [TestClass]
    public class getting_orphan_stories_in_a_jira_project
    {
        private const string EpicLink = "Epic Link";
        private const string ProjectKey = "AS";

        private List<JqOperator> _jqOperators;
        private List<string> _whereItems;
        private Mock<ILog> _mockLogger;

        [TestInitialize]
        public void getting_jira_stories()
        {
            var mockConnector = new Mock<IJiraConnector>();

            _jqOperators = new List<JqOperator>();
            _whereItems = new List<string>();
            mockConnector.Setup(x => x.GetSearchResults(It.IsAny<IList<JqOperator>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Action<string, Fields, Dictionary<string, object>>>()))
                .Callback<IList<JqOperator>, IEnumerable<string>, Action<string, Fields, Dictionary<string, object>>>((list, enumerable, func) =>
                {
                    _jqOperators.AddRange(list);
                    _whereItems.AddRange(enumerable);
                });
            _mockLogger = new Mock<ILog>();
            _mockLogger.Setup(x => x.Warn(It.IsAny<string>()));

            var jira = new Jira(mockConnector.Object, new MetaProject()
            {
                IssueTypes = new List<MetaIssueType>
                {
                    new MetaIssueType()
                    {
                        Name = "Story", Fields = new MetaField(){ Properties = new List<MetaProperty>
                        {
                            new MetaProperty(){Key = "customfield_10002", Property = "Story Points", Schema = "anything"}
                        } }
                    },
                    new MetaIssueType()
                    {
                        Name = "Epic", Fields = new MetaField(){ Properties = new List<MetaProperty>
                        {
                            new MetaProperty(){Key = "customfield_10005", Property = "Epic Link", Schema = "com.pyxis.greenhopper.jira:gh"},
                            new MetaProperty(){Key = "customfield_10006", Property = "Epic Name", Schema = "com.pyxis.greenhopper.jira:gh"}
                        } }
                    }
                }
            }, _mockLogger.Object);

            jira.GetStoriesWithNoEpicInProject(ProjectKey);
        }

        [TestMethod]
        public void should_have_three_jqOperators()
        {
            _jqOperators.Count.ShouldEqual(3);
        }

        [TestMethod]
        public void should_have_one_segment_for_project_key()
        {
            var op = _jqOperators.Single(x => x.Value == ProjectKey.QuoteReservedWord());
            op.Property.ShouldEqual("project");
        }

        [TestMethod]
        public void should_have_one_segment_for_epic_link()
        {
            var op = _jqOperators.Single(x => x.Value == JiraAdvancedSearch.Empty);
            op.Property.ShouldEqual(EpicLink.InQuotes());
        }

        [TestMethod]
        public void should_have_one_segment_for_issue_type()
        {
            var op = _jqOperators.Single(x => x.Value == "Story");
            op.Property.ShouldEqual("issuetype");
        }

        [TestMethod]
        public void should_never_call_the_logger()
        {
            _mockLogger.Verify(x => x.Warn(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void should_have_seven_query_items()
        {
            _whereItems.Count.ShouldEqual(10);
            _whereItems.Contains("issuetype").ShouldBeTrue();
            _whereItems.Contains("summary").ShouldBeTrue();
            _whereItems.Contains("description").ShouldBeTrue();
            _whereItems.Contains("priority").ShouldBeTrue();
            _whereItems.Contains("status").ShouldBeTrue();
            _whereItems.Contains("key").ShouldBeTrue();
            _whereItems.Contains("self").ShouldBeTrue();
            _whereItems.Contains("labels").ShouldBeTrue();
            _whereItems.Contains("timetracking").ShouldBeTrue();
            _whereItems.Contains("customfield_10002").ShouldBeTrue();
        }
    }


    [TestClass]
    public class deleting_epics_already_deleted
    {
        private const string IssueKey = "AKey-10";

        [TestMethod]
        public void Should_not_try_to_delete_it_again()
        {
            var mockConnector = new Mock<IJiraConnector>();
            mockConnector.Setup(x => x.GetSearchResults(It.IsAny<List<JqOperator>>(), It.IsAny<IEnumerable<string>>())
                ).Returns(new SearchResult
                {
                    ErrorMessages = new List<string> { "An issue with key 'AS-25' does not exist for field 'key'." }
                });
            var jira = new Jira(mockConnector.Object, string.Empty);

            jira.DeleteEpicIfExists(IssueKey);

            mockConnector.Verify(x => x.Delete(It.IsAny<string>(), It.IsAny<HttpStatusCode>(), It.IsAny<KeyValuePair<string, string>>()), Times.Never);
        }
    }
}