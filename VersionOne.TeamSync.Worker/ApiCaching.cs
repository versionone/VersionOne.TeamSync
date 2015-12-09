using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace VersionOne.TeamSync.JiraWorker
{
    public class ApiCaching
    {
        private IEnumerable<TEntity> GetFromCache<TEntity>(string key, Func<IEnumerable<TEntity>> valueFactory) where TEntity : class
        {
            var cache = MemoryCache.Default;
            var newValue = new Lazy<IEnumerable<TEntity>>(valueFactory);
            var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(30) };
            var value = cache.AddOrGetExisting(key, newValue, policy) as Lazy<IEnumerable<TEntity>>;
            return (value ?? newValue).Value; // Lazy<T> handles the locking itself
        }
    }
}
