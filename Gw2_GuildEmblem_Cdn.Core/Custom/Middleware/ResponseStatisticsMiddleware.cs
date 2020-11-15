using Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gw2_GuildEmblem_Cdn.Core.Extensions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Web.Http.Controllers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Gw2_GuildEmblem_Cdn.Core.Custom.Statistics;

namespace Gw2_GuildEmblem_Cdn.Core.Custom.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ResponseStatisticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IStatisticsUtility _statistics;

        public ResponseStatisticsMiddleware(RequestDelegate next, IStatisticsUtility statistics)
        {
            _next = next;
            _statistics = statistics;
        }

        public Task Invoke(HttpContext httpContext)
        {
            Task next = _next(httpContext);
            long age;

            if (httpContext.Response.Headers.ContainsKey(HeaderNames.Age) &&
                long.TryParse(httpContext.Response.Headers[HeaderNames.Age].Join(string.Empty), out age))
            {
                MethodInfo actionMethod = GetRequestedAction(httpContext);
                if (actionMethod != null && actionMethod.GetCustomAttribute<LogStatisticsAttribute>() != null)
                {
                    _statistics.RegisterResponseAsync(httpContext.Request, true);
                    _statistics.RegisterReferrerAsync(httpContext.Request, true);
                }
            }

            return next;
        }


        /// <summary>
        /// Tries to find the requested Action by request
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Methodinfo of requested action, null if unexpected request structure</returns>
        private MethodInfo GetRequestedAction(HttpContext context)
        {
            try
            {
                ControllerActionDescriptor controllerActionDescriptor = context .GetEndpoint().Metadata.GetMetadata<ControllerActionDescriptor>();
                return controllerActionDescriptor.MethodInfo;

            }
            catch { }
            return null;
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ResponseStatisticsMiddlewareExtensions
    {
        public static IApplicationBuilder UseResponseStatisticsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ResponseStatisticsMiddleware>();
        }
    }
}
