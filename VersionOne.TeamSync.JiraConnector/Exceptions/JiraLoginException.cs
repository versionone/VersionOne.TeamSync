﻿/*(c) Copyright 2018, CollabNet VersionOne, Inc. All rights reserved. (c)*/
namespace VersionOne.TeamSync.JiraConnector.Exceptions
{
    public class JiraLoginException : JiraException
    {
        public JiraLoginException() : base("Could not login with provided credentials", null) { }
        public JiraLoginException(string message) : base(message) { }
    }
}