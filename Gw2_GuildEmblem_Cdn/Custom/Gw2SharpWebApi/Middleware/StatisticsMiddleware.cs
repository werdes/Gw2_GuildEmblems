using Gw2_GuildEmblem_Cdn.Extensions;
using Gw2_GuildEmblem_Cdn.Utility;
using Gw2Sharp.WebApi.Http;
using Gw2Sharp.WebApi.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Custom.Gw2SharpWebApi.Middleware
{
    public class StatisticsMiddleware : IWebApiMiddleware
    {
        /// <summary>
        /// Wraps Statistics gathering around requests (cached/not cached, Endpoint)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="callNext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IWebApiResponse> OnRequestAsync(MiddlewareContext context, Func<MiddlewareContext, CancellationToken, Task<IWebApiResponse>> callNext, CancellationToken cancellationToken = default)
        {
            IWebApiResponse response = await callNext(context, cancellationToken);

            int skips = context.Request.Options.Url.AbsoluteUri.Contains("?") ? 0 : 1;
            string endpoint = context.Request.Options.Url.Segments.Reverse().Skip(skips).Reverse().Join(string.Empty);
            bool cached = response.GetCacheState() == CacheState.FromCache;

            StatisticsUtility.Instance.RegisterApiEndpointCallAsync(endpoint, cached);

            return response;
        }
    }
}