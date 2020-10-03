using Flatwhite.WebApi;
using Gw2_GuildEmblem_Cdn.Configuration;
using Gw2_GuildEmblem_Cdn.Model.Statistics;
using Gw2_GuildEmblem_Cdn.Model.Statistics.Output;
using Gw2_GuildEmblem_Cdn.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Gw2_GuildEmblem_Cdn.Controllers
{
    public class StatisticsController : ApiController
    {
        private const uint MAX_COUNT = 90;

        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private object _lock = new object();

        /// <summary>
        /// Returns statistics for a given number of days
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowCrossSiteJson]
        [Route("statistics/{count}")]
        [OutputCache(
            MaxAge = 60 /*Seconds*/ * 5 /*Minutes*/, // 5 Minutes
            StaleWhileRevalidate = 5,
            IgnoreRevalidationRequest = true,
            VaryByParam = "count")]
        public async Task<Statistics> Get(uint count)
        {
            Dictionary<DateTime, StatisticsContainer> containers = null;
            try
            {
                count = Math.Min(count, MAX_COUNT);
                containers = GetContainers(count);


                return new Statistics(containers);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            return null;
        }


        /// <summary>
        /// Returns the amount of created emblems in cache
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowCrossSiteJson]
        [Route("statistics/created_emblems")]
        [OutputCache(
            MaxAge = 60 /*Seconds*/ * 60 /*Minutes*/, // 60 Minutes
            StaleWhileRevalidate = 5,
            IgnoreRevalidationRequest = true,
            VaryByParam = "*")]
        public async Task<int> GetCountCreatedEmblems()
        {
            return await CacheUtility.Instance.GetCountEmblemsInCache();
        }


        /// <summary>
        /// Gets containers from the statistics archive, adds them to a dictionary by day
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private Dictionary<DateTime, StatisticsContainer> GetContainers(uint count)
        {
            Dictionary<DateTime, StatisticsContainer> retVal = new Dictionary<DateTime, StatisticsContainer>();

            lock(_lock)
            {
                List<(string, StatisticsContainer)> containers = StatisticsUtility.Instance.GetFromZip(count);

                foreach((string fileName, StatisticsContainer container) in containers)
                {
                    DateTime dateTime = DateTime.ParseExact(Path.GetFileNameWithoutExtension(fileName), "yyyy.MM.dd", System.Globalization.CultureInfo.CurrentCulture);
                    retVal.Add(dateTime, container);
                }
            }

            return retVal;
        }
    }
}
