/*(c) Copyright 2012, VersionOne, Inc. All rights reserved. (c)*/

namespace VersionOne.TeamSync.TfsConnector.Exceptions
{
    public class TfsLoginException : TfsException
    {
        public TfsLoginException() : base("Could not login with provided credentials", null) { }
        public TfsLoginException(string message) : base(message) { }
    }
}