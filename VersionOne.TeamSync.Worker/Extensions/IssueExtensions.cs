﻿using System;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker.Extensions
{
    public static class IssueExtensions
    {
        public static Epic ToV1Epic(this Issue issue)
        {
            return new Epic()
            {
                Name = issue.Fields.Summary,
                Description = issue.Fields.Description,
                Reference = issue.Key
            };
        }

        public static Story ToV1Story(this Issue issue, string v1ScopeId)
        {
            return new Story()
            {
                Name = issue.Fields.Summary,
                Description = issue.RenderedFields.Description,
                Estimate = issue.Fields.StoryPoints,
                ToDo = issue.Fields.RemainingInDays,
                Reference = issue.Key,
                ScopeId = v1ScopeId
            };
        }

        public static Defect ToV1Defect(this Issue issue, string v1ScopeId)
        {
            return new Defect()
            {
                Name = issue.Fields.Summary,
                Description = issue.RenderedFields.Description,
                Estimate = issue.Fields.StoryPoints,
                ToDo = issue.Fields.TimeTracking == null ? "" : Math.Abs(issue.Fields.TimeTracking.RemainingEstimateSeconds / 3600).ToString(),
                Reference = issue.Key,
                ScopeId = v1ScopeId
            };
        }

        public static bool ItMatchesStory(this Issue issue, Story story)
        {
            return string.Equals(story.Name, issue.Fields.Summary) &&
                string.Equals(story.Description, issue.RenderedFields.Description) &&
                string.Equals(story.Estimate, issue.Fields.StoryPoints) &&
                string.Equals(story.ToDo, issue.Fields.RemainingInDays) &&
                string.Equals(story.Reference, issue.Key) &&
                string.Equals(story.Super, issue.Fields.EpicLink);
        }


        public static bool ItMatchesDefect(this Issue issue, Defect defect)
        {
            return string.Equals(defect.Name, issue.Fields.Summary) &&
                string.Equals(defect.Description, issue.RenderedFields.Description.ToEmptyIfNull()) &&
                string.Equals(defect.Estimate, issue.Fields.StoryPoints.ToEmptyIfNull()) &&
                string.Equals(defect.ToDo, issue.Fields.RemainingInDays.ToEmptyIfNull()) &&
                string.Equals(defect.Reference, issue.Key) &&
                string.Equals(defect.Super, issue.Fields.EpicLink);
        }
    }
}
