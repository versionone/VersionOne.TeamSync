using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using VersionOne.TeamSync.JiraConnector.Entities;

namespace VersionOne.TeamSync.JiraWorker.Extensions
{
	public static class DictionaryExtensions
	{
		private static string _logMessage = "{0} field is not enabled in default issue screen for Jira project {1}. Estimate values cannot be synchronized";
	    public static void EvalLateBinding<T>(this IDictionary<string, T> properties, string issueKey, MetaProperty meta, Action<string> propertyToSetWithValue, ILog log)
	    {
	        if (meta.HasLoggedMissingProperty)
	            return;

	        if (meta.IsEmptyProperty)
	        {
                log.Warn(string.Format(_logMessage, meta.Key, issueKey.Split('-').First()));
	            meta.HasLoggedMissingProperty = true;
	            return; 
	        }

	        if (properties.ContainsKey(meta.Key) && properties[meta.Key] != null)
	            propertyToSetWithValue(properties[meta.Key].ToString());
	    }
	}
}
