using System;

namespace VersionOne.TeamSync.Worker.Extensions
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
    }
}
