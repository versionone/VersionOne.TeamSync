﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionOne.SDK.Jira.Connector;
using VersionOne.SDK.Jira.Entities;

namespace VersionOne.Integration.Service.Worker.Domain
{
    public class Jira
    {
        private readonly JiraConnector _connector;

        public Jira(JiraConnector connector)
        {
            _connector = connector;
        }

        internal async void CreateEpic(Epic epic) // TODO: async
        {
            //_connector.Post(JiraResource.Issue, epic.toJiraIssue());
        }

        internal async void UpdateEpic(Epic epic) // TODO: async
        {
        }

        internal async void ResolveEpic(Epic epic) // TODO: async
        {
        }

        internal async void DeleteEpic(Epic epic) // TODO: async
        {
        }
    }
}
