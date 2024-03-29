﻿using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VersionOne.TeamSync.JiraConnector.Tests
{
    [TestClass]
    public class JiraConnectorTests
    {
        private const string TestUser = "USER";
        private const string TestPassword = "PASSWORD";
        private readonly Connector.JiraConnector _connector = new Connector.JiraConnector("http://jira-6.cloudapp.net:8080/rest/api/latest", TestUser, TestPassword);

        [Ignore]
        [TestMethod]
        public void CreateEpicTest()
        {
            //http://jira-64.cloudapp.net:8080/plugins/servlet/restbrowser#/resource/api-2-issue/POST
            var epic = new
            {
                fields = new
                {
                    project = new { key = "OPC" },
                    summary = "This is an issue created by a unit test",
                    issuetype = new { name = "Epic" },
                    /*Epic Name*/
                    customfield_10104 = "Test Epic" // TODO: call createmeta first? app.config value?
                }
            };
            _connector.Post("api/latest/issue", epic, HttpStatusCode.Created);
        }

        [Ignore]
        [TestMethod]
        public void UpdateEpicTest()
        {
            //http://jira-64.cloudapp.net:8080/plugins/servlet/restbrowser#/resource/api-2-issue-issueidorkey/PUT
            // TODO: call editmeta? how do I know which fields/operations are available?
            var epicUpdate = new
            {
                update = new
                {
                    summary = new[]
                    {
                        new { set = string.Format("Description was set at: {0}", DateTime.Now) }
                    }
                }
            };
            _connector.Put("api/latest/issue/{issueIdOrKey}", epicUpdate, HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", "OPC-6"));
        }

        [Ignore]
        [TestMethod]
        public void ResolveEpicTest()
        {
            //http://jira-64.cloudapp.net:8080/plugins/servlet/restbrowser#/resource/api-2-issue-issueidorkey-transitions/POST
            var transition = new
            {
                transition = new { id = 31 } // id 31 == Done
            };

            _connector.Post("api/latest/issue/{issueIdOrKey}/transitions", transition, HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", "OPC-6"));
        }

        [Ignore]
        [TestMethod]
        public void DeleteEpicTest()
        {
            //http://jira-64.cloudapp.net:8080/plugins/servlet/restbrowser#/resource/api-2-issue-issueidorkey/DELETE
            _connector.Delete("api/latest/issue/{issueIdOrKey}", HttpStatusCode.NoContent, new KeyValuePair<string, string>("issueIdOrKey", "OPC-7"));
        }

        [Ignore]
        [TestMethod]
        public void GetIssueWorklogsTest()
        {
            //http://jira-6.cloudapp.net:8080/plugins/servlet/restbrowser#/resource/api-2-issue-issueidorkey-worklog
            _connector.Get("api/latest/issue/{issueIdOrKey}/worklog", new KeyValuePair<string, string>("issueIdOrKey", "STP-1"));
        }
    }
}