using System;

namespace VersionOne.TeamSync.Service.Worker.Extensions
{
    public static class StringExtensions
    {
        private static readonly string _inQuotes = "\"{0}\"";

        public static string InQuotes(this string value)
        {
            return String.Format(_inQuotes, value);
        }
    }
}
