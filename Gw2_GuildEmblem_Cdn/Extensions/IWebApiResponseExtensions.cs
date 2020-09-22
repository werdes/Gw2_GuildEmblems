using Gw2Sharp.WebApi.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Extensions
{
    public static class IWebApiResponseExtensions
    {
        private const string CACHE_STATE_HEADER_KEY = "X-Gw2Sharp-Cache-State";
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