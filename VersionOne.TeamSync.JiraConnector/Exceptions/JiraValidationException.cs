/*(c) Copyright 2018, CollabNet VersionOne, Inc. All rights reserved. (c)*/

using System;

namespace VersionOne.TeamSync.JiraConnector.Exceptions {
    public class JiraValidationException : JiraException {
        public JiraValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}