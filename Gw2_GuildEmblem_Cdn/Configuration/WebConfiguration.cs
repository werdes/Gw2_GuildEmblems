using Flatwhite.WebApi;
using Gw2_GuildEmblem_Cdn.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web;
using System.Web.Http;
using System.Web.Http.Routing;

namespace Gw2_GuildEmblem_Cdn.Configuration
{
    public class WebConfiguration
    {
        public static void Init()
        {

            InitContentNegitiationMethods();
            InitRoutes();
            InitCache();

            GlobalConfiguration.Configuration.EnsureInitialized();
        }

        private static void InitCache()
        {
            //GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configure(x => x.UseFlatwhiteCache(new FlatwhiteWebApiConfiguration
            {
                EnableStatusController = true,
                //LoopbackAddress = null // Set it to web server loopback address if server is behind firewall
            }));
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