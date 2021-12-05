using Gw2_GuildEmblem_Cdn.Core.Model.Statistics;
using Gw2_GuildEmblem_Cdn.Core.Model.Statistics.Output;
using Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Controllers
{
    public class StatisticsController : Controller
    {
        private const uint MAX_COUNT = 90;

        private readonly ILogger _log;
        private readonly IConfiguration _config;
        private readonly IEmblemCacheUtility _cache;
        private readonly IStatisticsUtility _statistics;
        private object _lock = new object();


        public StatisticsController(IConfiguration config, IEmblemCacheUtility cache, IStatisticsUtility statistics, ILogger<StatisticsController> log)
        {
            _config = config;
            _cache = cache;
            _statistics = statistics;
            _log = log;
        }

        /// <summary>
        /// Returns statistics for a given number of days
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableCors(Startup.AllowSpecificOriginsName)]
        [Route("statistics/{count}")]
        [ResponseCache(Duration = 60 /*Seconds*/ * 5 /*Minutes*/)]
        public async Task<IActionResult> Get(uint count)
        {
            Dictionary<DateTime, StatisticsContainer> containers = null;
            try
            {
                count = Math.Min(count, MAX_COUNT);
                containers = GetContainers(count);


                return Json(new Statistics(containers));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, count.ToString());
            }

            return StatusCode(500);
        }


        /// <summary>
        /// Returns the amount of created emblems in cache
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [EnableCors(Startup.AllowSpecificOriginsName)]
        [Route("statistics/created_emblems")]
        [ResponseCache(Duration = 60 /*Seconds*/ * 60 /*Minutes*/ * 24 /*Hours*/)]
        public async Task<int> GetCountCreatedEmblems()
        {
            return await _cache.GetCountEmblemsInCache();
        }


        /// <summary>
        /// Gets containers from the statistics archive, adds them to a dictionary by day
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private Dictionary<DateTime, StatisticsContainer> GetContainers(uint count)
        {
            Dictionary<DateTime, StatisticsContainer> retVal = new Dictionary<DateTime, StatisticsContainer>();

            lock (_lock)
            {
                List<(string, StatisticsContainer)> containers = _statistics.GetFromZip(count);

                foreach ((string fileName, StatisticsContainer container) in containers)
                {
                    DateTime dateTime = DateTime.ParseExact(Path.GetFileNameWithoutExtension(fileName), "yyyy.MM.dd", System.Globalization.CultureInfo.CurrentCulture);
                    retVal.Add(dateTime, container);
                }
            }

            return retVal;
        }
    }
}
