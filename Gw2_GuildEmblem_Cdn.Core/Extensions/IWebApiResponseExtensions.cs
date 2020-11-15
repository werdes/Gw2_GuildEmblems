using Gw2Sharp.WebApi.Http;
using System;

namespace Gw2_GuildEmblem_Cdn.Core.Extensions
{
    public static class IWebApiResponseExtensions
    {
        private const string CACHE_STATE_HEADER_KEY = "X-Gw2Sharp-Cache-State";

        /// <summary>
        /// Returns the Cache State of an API response
        /// </summary>
        /// <param name="apiResponse"></param>
        /// <returns></returns>
        public static CacheState GetCacheState(this IWebApiResponse apiResponse)
        {
            CacheState cacheState;
            if (apiResponse.ResponseHeaders.ContainsKey(CACHE_STATE_HEADER_KEY) &&
               Enum.TryParse(apiResponse.ResponseHeaders[CACHE_STATE_HEADER_KEY], out cacheState))
            {
                return cacheState;
            }
            return CacheState.FromLive;
        }
    }
}