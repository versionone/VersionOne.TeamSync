/*(c) Copyright 2012, VersionOne, Inc. All rights reserved. (c)*/

using System;
using System.Net;

namespace VersionOne.TeamSync.TfsConnector.Exceptions
{
    public class TfsException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public TfsException(string message) : base(message) { }
        public TfsException(string message, Exception innerException) : base(message, innerException) { }
        public TfsException(HttpStatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}