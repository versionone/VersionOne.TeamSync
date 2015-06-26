using System;
using VersionOne.TeamSync.JiraConnector.Entities;
using VersionOne.TeamSync.Worker.Domain;

namespace VersionOne.TeamSync.Worker.Extensions
{
    public static class IssueExtensions
    {
        public static Story ToV1Story(this Issue issue, string v1ScopeId)
        {
            return new Story()
            {
                Name = issue.Fields.Summary,
                Description = issue.RenderedFields.Description,
                Estimate = issue.Fields.StoryPoints,
                ToDo = issue.Fields.TimeTracking == null ? "" : Math.Abs(issue.Fields.TimeTracking.RemainingEstimateSeconds / 3600).ToString(),
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
    }
}
