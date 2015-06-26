/*(c) Copyright 2012, VersionOne, Inc. All rights reserved. (c)*/

using System;
using System.Net;

namespace VersionOne.TeamSync.JiraConnector.Exceptions
{
    public class JiraException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public JiraException(string message) : base(message) { }
        public JiraException(string message, Exception innerException) : base(message, innerException) { }
        public JiraException(HttpStatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}