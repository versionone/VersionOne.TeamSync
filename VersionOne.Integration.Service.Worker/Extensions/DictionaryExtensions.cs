using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionOne.Integration.Service.Worker.Extensions
{
	public static class DictionaryExtensions
	{
		public static T GetValueOrDefault<T>(this IDictionary<string, T> dictionary, string key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : default(T);
		}
	}
}
