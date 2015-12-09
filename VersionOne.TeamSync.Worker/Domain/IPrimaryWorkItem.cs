﻿using VersionOne.TeamSync.V1Connector.Interfaces;

namespace VersionOne.TeamSync.JiraWorker.Domain
{
    public interface IPrimaryWorkItem : IV1Asset
    {
        string ScopeId { get; set; }
        string ScopeName { get; set; }
        string Number { get; set; }
        string Reference { get; set; }
        string Priority { get; set; }
    }
}
