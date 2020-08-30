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
    public class RateLimiterMiddleware : Gw2Sharp.WebApi.Middleware.IWebApiMiddleware
    {
        private static RatelimitHandler _ratelimitHandler = new RatelimitHandler(100, nameof(RateLimiterMiddleware));

        public async Task<IWebApiResponse> OnRequestAsync(MiddlewareContext context, Func<MiddlewareContext, CancellationToken, Task<IWebApiResponse>> callNext, CancellationToken cancellationToken = default)
        {
            _ratelimitHandler.Wait();
            IWebApiResponse response = await callNext(context, cancellationToken);
            _ratelimitHandler.Set(response);

            return response;
        }
    }
}