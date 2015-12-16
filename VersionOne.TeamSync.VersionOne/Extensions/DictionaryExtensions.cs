using System.Collections.Generic;
using System.Linq;
using VersionOne.TeamSync.Core;

namespace VersionOne.TeamSync.VersionOne.Extensions
{
	public static class DictionaryExtensions
	{
		public static T GetValueOrDefault<T>(this IDictionary<string, T> dictionary, string key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : default(T);
		}

        public static T GetSingleRelationValueOrDefault<T>(this IDictionary<string, List<T>> dictionary, string key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key].SingleOrDefault() : default(T);
        }

        public static List<T> GetMultipleRelationValueOrDefault<T>(this IDictionary<string, List<T>> dictionary, string key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key].ToList() : default(List<T>);
        }

        public static string GetPlainTextFromHtmlOrDefault(this IDictionary<string, string> dictionary, string key)
        {
            if (!dictionary.ContainsKey(key))
                return string.Empty;

            var htmlValue = dictionary[key];

            var converter = new HtmlToPlainText();

            return converter.ConvertHtml(htmlValue);
        }
	}
}
