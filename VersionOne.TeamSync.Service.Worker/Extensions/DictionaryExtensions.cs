using System.Collections.Generic;

namespace VersionOne.TeamSync.Service.Worker.Extensions
{
	public static class DictionaryExtensions
	{
		public static T GetValueOrDefault<T>(this IDictionary<string, T> dictionary, string key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : default(T);
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
