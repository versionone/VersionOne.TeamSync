using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using VersionOne.Integration.Service.Core.Tests.Helpers;

namespace VersionOne.Integration.Service.Core.Tests
{
	[TestFixture]
	public class when_we_consume_an_issue
	{
		private Issue issue;
		private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
		{
			NullValueHandling = NullValueHandling.Ignore
		};
		[TestFixtureSetUp]
		public void setup()
		{
			issue = JsonConvert.DeserializeObject<Issue>(ContentResponses.Jira.FullIssue, _serializerSettings);
		}

		[Test]
		public void has_id_stuff_set()
		{
			issue.Key.ShouldNotBeEmpty();
			issue.Self.ShouldNotBeEmpty();
			issue.id.ShouldNotBeEmpty();
		}

		[Test]
		public void has_summary()
		{
			issue.Fields.Summary.ShouldNotBeEmpty();
		}

		[Test]
		public void has_description()
		{
			issue.Fields.Description.ShouldNotBeEmpty();
		}

		[Test]
		public void has_priority()
		{
			issue.Fields.Priority.ShouldNotBeNull();
		}

		[Test]
		public void has_a_status()
		{
			issue.Fields.Status.Name.ShouldNotBeEmpty();
		}

		[Test]
		public void has_an_OriginalEstimate()
		{
			//issue.Fields.TimeOriginalEstimate.ShouldNotEqual(0);
		}
	}


	[TestFixture]
	public class JiraApiErrorResults
	{
		[Test]
		public async void when_an_error_is_throw_should_populate_the_base_class_with_the_given_error()
		{
			var fakeHttp = new MockHttp();
			fakeHttp.AddJsonResponse("http://jira-6.cloudapp.net:8080/rest/api/2/search?jql=project=JIT AND issueType=Epic&fields=issueType,summary,timeoriginalestimate,description,status,key,self", "{\"errorMessages\":[\"The value 'JIT' does not exist for the field 'project'.\"],\"errors\":{}}");

			var connector = new JiraConnector(fakeHttp);
			var result = await connector.GetProject("JIT");
			result.errorMessages.Count.ShouldEqual(1);
		}

		[Test]
		public async void when_the_project_exists_a_bunch_of_issues_should_be_returned()
		{
			var fakeHttp = new MockHttp();
			fakeHttp.AddJsonResponse("http://jira-6.cloudapp.net:8080/rest/api/2/search?jql=project=JIT AND issueType=Epic&fields=issueType,summary,timeoriginalestimate,description,status,key,self", ContentResponses.Jira.ProjectSuccessful);
			var connector = new JiraConnector(fakeHttp);
			var result = await connector.GetProject("JIT");
			result.Issues.Count.ShouldEqual(3);
		}
	}
}
