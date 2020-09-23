using Flatwhite;
using Flatwhite.WebApi;
using Gw2_GuildEmblem_Cdn.Configuration;
using Gw2_GuildEmblem_Cdn.Controllers;
using Gw2_GuildEmblem_Cdn.Custom.FlatwhiteCache;
using Gw2_GuildEmblem_Cdn.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Routing;

namespace Gw2_GuildEmblem_Cdn.Configuration
{
    public class WebConfiguration
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Init()
        {

            InitContentNegitiationMethods();
            InitPaths();
            InitRoutes();
            InitCache();

            GlobalConfiguration.Configuration.EnsureInitialized();
        }

        /// <summary>
        /// Initializes the stat directories (create if not exist)
        /// </summary>
        private static void InitPaths()
        {
            InitPath(CacheUtility.EMBLEM_CACHE_DIRECTORY_NAME);
            InitPath(StatisticsUtility.STATISTICS_DIRECTORY_NAME);
        }

        /// <summary>
        /// Checks if a directory exists, creates it otherwise
        /// </summary>
        /// <param name="dir"></param>
        private static void InitPath(string dir)
        {
            string path = Path.Combine(ConfigurationManager.AppSettings["cache_path"], dir);
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Configures the flatwhite cache
        /// </summary>
        private static void InitCache()
        {
            EventCacheResponseBuilder cacheResponseBuilder = new EventCacheResponseBuilder();
            cacheResponseBuilder.OnResponse += CacheResponseBuilder_OnResponse;

            GlobalConfiguration.Configure(x => x.UseFlatwhiteCache(new FlatwhiteWebApiConfiguration
            {
                EnableStatusController = true,
                ResponseBuilder = cacheResponseBuilder
            }));

            Global.Logger = new FlatwhiteLog4netLogger();
        }

        /// <summary>
        /// Called once a request has been served from cache
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CacheResponseBuilder_OnResponse(object sender, CacheResponseEventArgs e)
        {
            StatisticsUtility.Instance.RegisterResponseAsync(e.Request, e.Cached);
            StatisticsUtility.Instance.RegisterReferrerAsync(e.Request, e.Cached);
        }

        private static void InitRoutes()
        {
            GlobalConfiguration.Configuration.MapHttpAttributeRoutes();
        }

        private static void InitContentNegitiationMethods()
        {
            JsonMediaTypeFormatter oJsonFormatter = new JsonMediaTypeFormatter();
            oJsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            GlobalConfiguration.Configuration.Services.Replace(typeof(IContentNegotiator), new JsonContentNegotiator(oJsonFormatter));
        }
    }
}