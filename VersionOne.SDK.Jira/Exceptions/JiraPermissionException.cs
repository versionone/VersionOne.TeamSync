/*(c) Copyright 2012, VersionOne, Inc. All rights reserved. (c)*/

using System;

namespace VersionOne.SDK.Jira.Exceptions {
    public class JiraPermissionException : JiraException {
        public JiraPermissionException(string message, Exception innerException) : base(message, innerException) { }
    }
}