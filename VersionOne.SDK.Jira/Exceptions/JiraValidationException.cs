/*(c) Copyright 2012, VersionOne, Inc. All rights reserved. (c)*/

using System;

namespace VersionOne.SDK.Jira.Exceptions {
    public class JiraValidationException : JiraException {
        public JiraValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}