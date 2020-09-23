using Flatwhite.WebApi;
using Gw2_GuildEmblem_Cdn.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using System.Web.Routing;

namespace Gw2_GuildEmblem_Cdn.Custom.FlatwhiteCache
{
    public class EventCacheResponseBuilder : CacheResponseBuilder
    {
        public event EventHandler<CacheResponseEventArgs> OnResponse;
        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override HttpResponseMessage GetResponse(CacheControlHeaderValue cacheControl, WebApiCacheItem cacheItem, HttpRequestMessage request)
        {
            //Try to get the Action method called by the request to determine if Statistic Logging is activated for it
            MethodInfo actionMethod = GetRequestedAction(request);
            if (actionMethod != null && actionMethod.GetCustomAttribute<LogStatisticsAttribute>() != null)
            {
                OnResponse?.Invoke(this, new CacheResponseEventArgs()
                {
                    Cached = cacheItem != null,
                    Request = request
                });
            }


            HttpResponseMessage response = base.GetResponse(cacheControl, cacheItem, request);

            //Add the Emblem-Status header to the cached response
            if (cacheItem.ResponseHeaders.Contains(Controllers.EmblemController.EMBLEM_STATUS_HEADER_KEY))
                response.Headers.Add(Controllers.EmblemController.EMBLEM_STATUS_HEADER_KEY, cacheItem.ResponseHeaders.Where(x => x.Key == Controllers.EmblemController.EMBLEM_STATUS_HEADER_KEY).FirstOrDefault().Value);

            return response;

        }

        /// <summary>
        /// Tries to find the requested Action by request
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Methodinfo of requested action, null if unexpected request structure</returns>
        private MethodInfo GetRequestedAction(HttpRequestMessage request)
        {
            try
            {
                IHttpRouteData routeData = request.GetRouteData();
                HttpActionDescriptor[] actionDescriptors = (HttpActionDescriptor[])routeData.Route.DataTokens["actions"];
                ReflectedHttpActionDescriptor reflectedHttpActionDescriptor = (ReflectedHttpActionDescriptor)actionDescriptors.First();

                return reflectedHttpActionDescriptor.MethodInfo;
            }
            catch { }
            return null;
        }
    }
}