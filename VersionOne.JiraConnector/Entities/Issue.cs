﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace VersionOne.JiraConnector.Entities
{
    public abstract class JiraBase
    {
        protected JiraBase()
        {
            ErrorMessages = new List<string>();
        }
        public List<string> ErrorMessages { get; set; }
        public Dictionary<string, string> Errors { get; set; }

        public bool HasErrors { get { return ErrorMessages.Count > 0; } }
    }

	public class BadResult : JiraBase
	{
		//for just mess ups
	}

    public class Errors
    {
        string Status { get; set; }
    }

    public class JiraProject : JiraBase
    {
        public int MaxResults { get; set; }
        public List<Issue> Issues { get; set; }
    }
    
    public class SearchResult : JiraBase
    {
        public SearchResult()
        {
            issues = new List<Issue>();
        }
        public string Expand { get; set; }
        public int StartAt { get; set; }
        public int maxResults { get; set; }
        public int total { get; set; }

        public List<Issue> issues { get; set; }
    }

    public class ItemBase
    {
        public string id { get; set; }
        public string Key { get; set; }
        public string Self { get; set; }

	    public bool IsEmpty
	    {
		    get { return string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(Key) && string.IsNullOrWhiteSpace(Self); }
	    }
    }

    public class BaseRefType : ItemBase
    {
        public string Name { get; set; }
    }

    public class Issue : ItemBase
    {
        public string Expand { get; set; }

        public Fields Fields { get; set; }
    }

    public class Priority
    {
        public string Name { get; set; }
    }

    public class Status
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public BaseRefType StatusCategory { get; set; }
    }

    public class IssueType
    {
        public string Name { get; set; }
    }

    public class ProgressObj
    {
        public int Progress { get; set; }
        public int Total { get; set; }
        public float Percent { get; set; }
    }


    public class EpicFields : Fields
    {
        [JsonProperty("customfield_10006")] //yea, seriously
        public string Name { get; set; }
    }

    public class Fields
    {
        public string Summary { get; set; }

        //public int TimeEstimate { get; set; }
        //public int TimeOriginalEstimate { get; set; }  //in seconds, 1d == 28800s, 8 hour day
        //public int AggregateTimeEstimate { get; set; }

        public string Description { get; set; }

        public ProgressObj Progress { get; set; }
        public ProgressObj AggregateProgress { get; set; }

        public IssueType IssueType { get; set; }
        public Status Status { get; set; }
        public Priority Priority { get; set; }
        public Project Project { get; set; }
    }

    public class Project
    {
        public string Key { get; set; }
    }
}