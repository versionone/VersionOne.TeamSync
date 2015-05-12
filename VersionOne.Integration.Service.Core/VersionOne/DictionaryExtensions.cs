using System.Collections.Generic;

namespace VersionOne.Integration.Service.Core.VersionOne
{
	public static class DictionaryExtensions
	{
		public static T GetValueOrDefault<T>(this IDictionary<string, T> dictionary, string key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : default(T);
		}
	}
}