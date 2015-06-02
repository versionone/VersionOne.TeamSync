/*(c) Copyright 2012, VersionOne, Inc. All rights reserved. (c)*/

using System;

namespace VersionOne.TeamSync.JiraConnector.Exceptions {
    public class JiraPermissionException : JiraException {
        public JiraPermissionException(string message, Exception innerException) : base(message, innerException) { }
    }
}