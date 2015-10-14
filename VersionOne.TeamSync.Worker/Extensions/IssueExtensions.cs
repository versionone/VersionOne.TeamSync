using System;
using System.Collections.Generic;
using System.Globalization;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker.Extensions
{
    public static class IssueExtensions
    {
        public static Epic ToV1Epic(this Issue issue)
        {
            return new Epic
            {
                Name = issue.Fields.Summary,
                Description = issue.Fields.Description,
                Reference = issue.Key
            };
        }

        public static Story ToV1Story(this Issue issue, string v1ScopeId, string priorityId, string statusId)
        {
            return new Story
            {
                Name = issue.Fields.Summary,
                Description = issue.RenderedFields.Description,
                Estimate = issue.Fields.StoryPoints,
                ToDo = issue.Fields.TimeTracking == null ? "" : Math.Abs(issue.Fields.TimeTracking.RemainingEstimateSeconds / 3600).ToString(),
                Reference = issue.Key,
                ScopeId = v1ScopeId,
                Priority = priorityId,
                Status = statusId
            };
        }

        public static Defect ToV1Defect(this Issue issue, string v1ScopeId, string priorityId, string statusId)
        {
            return new Defect
            {
                Name = issue.Fields.Summary,
                Description = issue.RenderedFields.Description,
                Estimate = issue.Fields.StoryPoints,
                ToDo = issue.Fields.TimeTracking == null ? "" : Math.Abs(issue.Fields.TimeTracking.RemainingEstimateSeconds / 3600).ToString(),
                Reference = issue.Key,
                ScopeId = v1ScopeId,
                Priority = priorityId,
                Status = statusId
            };
        }

        public static Actual ToV1Actual(this Worklog worklog, string memberId, string v1ScopeId, string workItemId)
        {
            const decimal secondsInHour = 3600;
            return new Actual
            {
                Date = worklog.started,
                Value = (worklog.timeSpentSeconds / secondsInHour).ToString(CultureInfo.InvariantCulture),
                Reference = worklog.id.ToString(),
                MemberId = memberId,
                ScopeId = v1ScopeId,
                WorkItemId = workItemId
            };
        }

        public static bool ItMatchesStory(this Issue issue, Story story)
        {
            return string.Equals(story.Name, issue.Fields.Summary.Trim()) &&
                   string.Equals(story.Description, issue.RenderedFields.Description.ToEmptyIfNull()) &&
                   string.Equals(story.Estimate, issue.Fields.StoryPoints.ToEmptyIfNull()) &&
                   string.Equals(story.ToDo, issue.Fields.RemainingInDays.ToEmptyIfNull()) &&
                   string.Equals(story.Reference, issue.Key) &&
                   string.Equals(story.SuperNumber, issue.Fields.EpicLink.ToEmptyIfNull());
        }

        public static bool ItMatchesDefect(this Issue issue, Defect defect)
        {
            return string.Equals(defect.Name, issue.Fields.Summary.Trim()) &&
                   string.Equals(defect.Description, issue.RenderedFields.Description.ToEmptyIfNull()) &&
                   string.Equals(defect.Estimate, issue.Fields.StoryPoints.ToEmptyIfNull()) &&
                   string.Equals(defect.ToDo, issue.Fields.RemainingInDays.ToEmptyIfNull()) &&
                   string.Equals(defect.Reference, issue.Key) &&
                   string.Equals(defect.SuperNumber, issue.Fields.EpicLink.ToEmptyIfNull());
        }

        public static bool HasAssignee(this Issue issue)
        {
            return issue.Fields.Assignee != null;
        }

        public static bool ItMatchesMember(this User assignee, Member member)
        {
            return string.Equals(member.Name, assignee.displayName) &&
                   string.Equals(member.Nickname, assignee.name) &&
                   string.Equals(member.Email, assignee.emailAddress);
        }

        public static Member ToV1Member(this User assignee)
        {
            return new Member
            {
                Name = assignee.displayName,
                Nickname = assignee.name,
                Email = assignee.emailAddress
            };
        }
    }
}