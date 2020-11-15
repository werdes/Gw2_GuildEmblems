using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Custom.Middleware
{
    public class IgnoreClientResponseCacheMiddleware
    {
        private const string CACHE_CONTROL_HEADER_KEY = "cache-control";
        private const string PRAGMA_HEADER_KEY = "pragma";

        private readonly RequestDelegate _next;

        public IgnoreClientResponseCacheMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            List<KeyValuePair<string, StringValues>> cacheControlHeaders = 
                httpContext.Request.Headers.Where(x => x.Key.Trim().ToLower() == CACHE_CONTROL_HEADER_KEY &&
                                                         x.Value.Count > 0 &&
                                                         (
                                                         x.Value.Aggregate((y, z) => y + z).Trim().ToLower() == "no-cache" ||
                                                         x.Value.Aggregate((y, z) => y + z).Trim().ToLower() == "no-store" ||
                                                         x.Value.Aggregate((y, z) => y + z).Trim().ToLower() == "max-age=0"
                                                         )
                                                 ).ToList();
            foreach (var cacheControlHeader in cacheControlHeaders)
            {
                httpContext.Request.Headers.Remove(cacheControlHeader);
            }

            List<KeyValuePair<string, StringValues>> pragmaNoCacheHeaders = 
                httpContext.Request.Headers.Where(x => x.Key.Trim().ToLower() == PRAGMA_HEADER_KEY &&
                                                     x.Value.Count > 0 &&
                                                     x.Value.Aggregate((y, z) => y + z).Trim().ToLower() == "no-cache"
                                                 ).ToList();

            foreach (var pragmaNoCacheHeader in pragmaNoCacheHeaders)
            {
                httpContext.Request.Headers.Remove(pragmaNoCacheHeader);
            }

            Task next = _next(httpContext);
            return next;
        }
    }

    public static class IgnoreClientResponseCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseIgnoreClientResponseCache(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IgnoreClientResponseCacheMiddleware>();
        }
    }
}