using Gw2_GuildEmblem_Cdn.Core.Extensions;
using Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces;
using Gw2Sharp.WebApi.Http;
using Gw2Sharp.WebApi.Middleware;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Custom.Gw2SharpWebApi.Middleware
{
    public class StatisticsMiddleware : IWebApiMiddleware
    {
        private readonly IStatisticsUtility _statistics;

        public StatisticsMiddleware(IStatisticsUtility statistics)
        {
            _statistics = statistics;
        }

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

            _statistics.RegisterApiEndpointCallAsync(endpoint, cached);

            return response;
        }
    }
}