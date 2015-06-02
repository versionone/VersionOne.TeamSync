/*(c) Copyright 2012, VersionOne, Inc. All rights reserved. (c)*/

using System;

namespace VersionOne.TeamSync.JiraConnector.Exceptions {
    public class JiraException : Exception {
        public JiraException(string message, Exception innerException) : base(message, innerException) { }
    }
}