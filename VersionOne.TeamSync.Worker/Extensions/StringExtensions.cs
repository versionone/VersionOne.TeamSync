using System;
using System.Linq;
using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.JiraWorker.Extensions
{
    public static class StringExtensions
    {
        private static readonly string _inQuotes = "\"{0}\"";

        public static string InQuotes(this string value)
        {
            return String.Format(_inQuotes, value);
        }

        public static string ToEmptyIfNull(this string value)
        {
            return value ?? "";
        }

        public static string ToEmptyIfNull(this IJiraRelation jiraRelation)
        {
            return jiraRelation != null ? jiraRelation.Name : "";
        }

        public static bool Is(this string value, string[] items)
        {
            return items.Contains(value);
        }
    }
}
