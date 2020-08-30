using Gw2Sharp.WebApi.Caching;
using Gw2Sharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;


namespace Gw2_GuildEmblem_Cdn.Custom.Gw2SharpWebApi.Caching
{
    public class DelayedExpiryMemoryCacheMethod : MemoryCacheMethod
    {
        public TimeSpan ExpiryDelay { get; set; }

        public DelayedExpiryMemoryCacheMethod(TimeSpan expiryDelay) : this(expiryDelay, 5 * 60 * 1000)
        {
        }

        public DelayedExpiryMemoryCacheMethod(TimeSpan expiryDelay, int gcTimeout) : base(gcTimeout)
        {
            ExpiryDelay = expiryDelay;
        }


        /// <summary>
        /// Same Implementation from BaseCacheMethod, but w/ ignoring expired header
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="category"></param>
        /// <param name="id"></param>
        /// <param name="updateFunc"></param>
        /// <returns></returns>
        public override Task<CacheItem<T>> GetOrUpdateAsync<T>(string category, string id, Func<Task<(T, DateTimeOffset)>> updateFunc)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (updateFunc == null)
                throw new ArgumentNullException(nameof(updateFunc));
            return ExecAsync();

            async Task<CacheItem<T>> ExecAsync()
            {
                var fItem = await this.TryGetAsync<T>(category, id).ConfigureAwait(false);
                if (fItem != null)
                    return fItem;

                var (item, expiryTime) = await updateFunc().ConfigureAwait(false);
                var cache = new CacheItem<T>(category, id, item, expiryTime + ExpiryDelay);
                await this.SetAsync(cache).ConfigureAwait(false);
                return cache;
            }
        }

        /// <summary>
        /// Same Implementation from BaseCacheMethod, but w/ ignoring expired header
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="category"></param>
        /// <param name="ids"></param>
        /// <param name="updateFunc"></param>
        /// <returns></returns>
        public override Task<IList<CacheItem<T>>> GetOrUpdateManyAsync<T>(
            string category,
            IEnumerable<string> ids,
            Func<IList<string>, Task<(IDictionary<string, T>, DateTimeOffset)>> updateFunc)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            if (updateFunc == null)
                throw new ArgumentNullException(nameof(updateFunc));
            return ExecAsync();

            async Task<IList<CacheItem<T>>> ExecAsync()
            {
                var idsList = ids as IList<string> ?? ids.ToList();

                var cache = await this.GetManyAsync<T>(category, idsList).ConfigureAwait(false);
                IList<string> missing = idsList.Except(cache.Keys).ToList();

                if (missing.Count > 0)
                {
                    var (newItems, expiryTime) = await updateFunc(missing).ConfigureAwait(false);
                    IList<CacheItem<T>> newCacheItems = newItems.Select(x => new CacheItem<T>(category, x.Key, x.Value, expiryTime + ExpiryDelay)).ToList();
                    await this.SetManyAsync(newCacheItems).ConfigureAwait(false);
                    foreach (var item in newCacheItems)
                        cache[item.Id] = item;
                }

                // Return in the same order as requested
                return idsList
                    .Select(x => cache.TryGetValue(x, out var value) ? value : null)
                    .Where(x => !(x is null))
                    .ToList();
            }
        }
    }
}