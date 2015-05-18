using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.Integration.Service.Worker.Extensions
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
