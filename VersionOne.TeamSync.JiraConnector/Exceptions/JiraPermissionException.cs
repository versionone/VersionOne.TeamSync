/*(c) Copyright 2018, CollabNet VersionOne, Inc. All rights reserved. (c)*/

using System;

namespace VersionOne.TeamSync.JiraConnector.Exceptions
{
    public class JiraPermissionException : JiraException
    {
        public JiraPermissionException(string message, Exception innerException) : base(message, innerException) { }
    }
}