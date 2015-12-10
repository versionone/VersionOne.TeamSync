using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.JiraWorker.Extensions
{
    public static class StringExtensions
    {
        public static string ToEmptyIfNull(this IJiraRelation jiraRelation)
        {
            return jiraRelation != null ? jiraRelation.Name : "";
        }
    }
}
