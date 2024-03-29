﻿using Gw2_GuildEmblem_Cdn.Core.Utility;
using Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces;
using Gw2Sharp.WebApi.Http;
using Gw2Sharp.WebApi.Middleware;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Custom.Gw2SharpWebApi.Middleware
{
    public class RateLimiterMiddleware : Gw2Sharp.WebApi.Middleware.IWebApiMiddleware
    {
        private IStatisticsUtility _statistics;
        private ILogger _log;
        private static RatelimitHandler _ratelimitHandler;

        public RateLimiterMiddleware(IStatisticsUtility statistics, ILogger log)
        {
            _statistics = statistics;
            _log = log;

            _ratelimitHandler = new RatelimitHandler(_statistics, _log, 100, nameof(RateLimiterMiddleware));
        }




        /// <summary>
        /// Wraps ratelimit handling around every request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="callNext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IWebApiResponse> OnRequestAsync(MiddlewareContext context, Func<MiddlewareContext, CancellationToken, Task<IWebApiResponse>> callNext, CancellationToken cancellationToken = default)
        {
            _ratelimitHandler.Wait();
            IWebApiResponse response = await callNext(context, cancellationToken);
            _ratelimitHandler.Set(response);

            return response;
        }
    }
}