using System;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;

namespace VersionOne.TeamSync.JiraConnector.Entities
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

        /// <summary>
        /// HTML formatted fields
        /// </summary>
        public RenderedFields RenderedFields { get; set; }
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

    public class Fields
    {
        public Fields()
        {
            Labels = new List<string>();
        }
        public string Summary { get; set; }

        public string Description { get; set; }
        public List<string> Labels { get; set; }

        public ProgressObj Progress { get; set; }
        public ProgressObj AggregateProgress { get; set; }

        public IssueType IssueType { get; set; }
        public Status Status { get; set; }
        public Priority Priority { get; set; }
        public Project Project { get; set; }
        public User Assignee { get; set; }

        public TimeTracking TimeTracking { get; set; }

        public string RemainingInDays
        {
            get { return TimeTracking == null ? null : Math.Abs(TimeTracking.RemainingEstimateSeconds / 3600).ToString(); }
        }

        //late binding properties
        public string StoryPoints { get; set; }
        public string EpicLink { get; set; }
        public IEnumerable<Sprint> Sprints { get; set; }
    }

    public class RenderedFields
    {
        public string Description { get; set; }
    }

    public class Project
    {
        public string Key { get; set; }
    }

    public class TimeTracking
    {
        public string RemainingEstimate { get; set; }
        public string TimeSpent { get; set; }
        public int RemainingEstimateSeconds { get; set; }
        public int TimeSpentSeconds { get; set; }
    }

    public class Worklog
    {
        public string self;
        public User author;
        public User updateAuthor;
        public string comment;
        public DateTime created;
        public DateTime updated;
        public DateTime started;
        public string timeSpent;
        public long timeSpentSeconds;
        public int id;
    }

    public class User
    {
        public string self;
        public string name;
        public string key;
        public string emailAddress;
        //public something avatarUrls 48x48, 24x24, 16x16, 32x32;
        public string displayName;
        public bool active;
        public string timeZone;
    }

    public class Sprint
    {
        public int id;
        public int rapidViewId;
        public string state;
        public string name;
        public DateTime? startDate;
        public DateTime? completeDate;
        public int sequence;
    }
}