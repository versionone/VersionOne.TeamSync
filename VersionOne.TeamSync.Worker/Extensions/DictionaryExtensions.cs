using System;
using System.Collections.Generic;
using log4net;
using Microsoft.SqlServer.Server;
using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.Worker.Extensions
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

        private static string _logMessage = "Unable to find {0} on the screen. Add this to the default display to enable this property";
	    public static void EvalLateBinding<T>(this IDictionary<string, T> properties, MetaProperty meta, Action<string> propertyToSetWithValue, ILog log)
	    {
	        if (meta.IsEmptyProperty)
	        {
                log.Warn(string.Format(_logMessage, meta.Key));
	            return; 
	        }

	        if (properties.ContainsKey(meta.Key) && properties[meta.Key] != null)
	            propertyToSetWithValue(properties[meta.Key].ToString());
	    }
	}
}
