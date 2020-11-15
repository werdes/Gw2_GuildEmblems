using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Gw2_GuildEmblem_Cdn.Core.Model.Statistics.Output
{
    public class Statistics
    {
        private const int TOPLIST_SIZE = 5;
        private readonly string[] REFERRER_BLACKLIST = new string[]
        {
            "beta.",
            "intern."
        };

        public enum CacheState
        {
            FromCache,
            FromLive
        }

        [JsonIgnore]
        public Dictionary<DateTime, StatisticsContainer> Containers { get; set; }

        public Statistics(Dictionary<DateTime, StatisticsContainer> containers)
            => Containers = containers;

        //[JsonProperty("total_cached_variants")]
        //public int TotalCachedVariants => CacheUtility.Instance.GetCountEmblemsInCache();

        [JsonPropertyName("total_requests_served")]
        public Dictionary<CacheState, int> TotalRequestServed =>
            Containers
                .Select(x => x.Value)
                .Select(x => x.Responses)
                .Select(x => x.SelectMany(y => y.Value))
                .SelectMany(x => x)
                .GroupBy(x => x.Value.ServedFromCache)
                .ToDictionary(
                    k => k.Key ? CacheState.FromCache : CacheState.FromLive,
                    v => v.Sum(z => z.Value.Count)
                );

        [JsonPropertyName("total_days")]
        public int TotalDays => Containers.Keys.Count;

        /// <summary>
        /// Returns Servings by cachestate
        /// </summary>
        [JsonPropertyName("cache_servings")]
        public Dictionary<string, Dictionary<CacheState, int>> CacheServings
        {
            get
            {
                Dictionary<string, Dictionary<CacheState, int>> retVal = new Dictionary<string, Dictionary<CacheState, int>>();

                foreach (DateTime date in Containers.Keys)
                {
                    string dateFormatted = date.ToString("yyyy.MM.dd");
                    retVal.Add(dateFormatted, new Dictionary<CacheState, int>());
                    retVal[dateFormatted].Add(CacheState.FromCache, Containers[date].Responses.Values.SelectMany(x => x.Values).Where(x => x.ServedFromCache).Sum(x => x.Count));
                    retVal[dateFormatted].Add(CacheState.FromLive, Containers[date].Responses.Values.SelectMany(x => x.Values).Where(x => !x.ServedFromCache).Sum(x => x.Count));
                }

                return retVal;
            }
        }


        /// <summary>
        /// Returns the most served sizes
        /// </summary>
        [JsonPropertyName("sizes")]
        public Dictionary<int, int> SizeServings =>
            Containers
                .Select(x => x.Value.Responses)
                .SelectMany(x => x.Values)
                .SelectMany(x => x.Values)
                .GroupBy(x => x.Size)
                .ToDictionary(k => k.Key, v => v.Sum(y => y.Count));



        /// <summary>
        /// Returns the 25 most served guilds
        /// </summary>
        [JsonPropertyName("guild_servings")]
        public Dictionary<string, Dictionary<Guid, int>> GuildServings =>
            Containers.Select(x => new KeyValuePair<string, Dictionary<Guid, int>>(
                x.Key.ToString("yyyy.MM.dd"),
                x.Value.Responses
                    .SelectMany(y => y.Value)
                    .Select(y => y.Value)
                    .GroupBy(y => y.GuildId)
                    .ToDictionary(k => k.Key, v => v.Sum(y => y.Count))
                    .OrderByDescending(y => y.Value)
                    .Take(TOPLIST_SIZE)
                    .ToDictionary(k => k.Key, v => v.Value)
                )
            ).ToDictionary(k => k.Key, v => v.Value);

        /// <summary>
        /// Returns the 25 most served guilds ever
        /// </summary>
        [JsonPropertyName("guild_servings_total")]
        public Dictionary<Guid, int> GuildServingsTotal =>
            Containers.SelectMany(x => x.Value.Responses)
                    .Select(y => y.Value)
                    .SelectMany(y => y.Values)
                    .GroupBy(y => y.GuildId)
                    .ToDictionary(k => k.Key, v => v.Sum(y => y.Count))
                    .OrderByDescending(y => y.Value)
                    .Take(TOPLIST_SIZE)
                    .ToDictionary(k => k.Key, v => v.Value);



        /// <summary>
        /// Foregrounds used
        /// </summary>
        [JsonPropertyName("foregrounds")]
        public Dictionary<int, int> Foregrounds =>
           Containers.Values
                .SelectMany(x => x.EmblemCreations.Values)
                .Where(y => y.Emblem != null)
                .GroupBy(y => y.Emblem.Foreground.Id)
                .ToDictionary(k => k.Key, v => v.Count())
                .OrderByDescending(y => y.Value)
                .Take(TOPLIST_SIZE)
                .ToDictionary(k => k.Key, v => v.Value);

        /// <summary>
        /// Backgrounds used
        /// </summary>
        [JsonPropertyName("backgrounds")]
        public Dictionary<int, int> Backgrounds =>
           Containers.Values
                .SelectMany(x => x.EmblemCreations.Values)
                .Where(y => y.Emblem != null)
                .GroupBy(y => y.Emblem.Background.Id)
                .ToDictionary(k => k.Key, v => v.Count())
                .OrderByDescending(y => y.Value)
                .Take(TOPLIST_SIZE)
                .ToDictionary(k => k.Key, v => v.Value);


        /// <summary>
        /// Returns requests by referrers
        /// </summary>
        [JsonPropertyName("referrers")]
        public Dictionary<string, Dictionary<string, int>> Referrers =>
             Containers.Select(x => new KeyValuePair<string, Dictionary<string, int>>(
                x.Key.ToString("yyyy.MM.dd"),
                x.Value.Referrers
                    .Select(y => y.Value)
                    .Select(y => y
                        .Where(z => !REFERRER_BLACKLIST.Any(a => z.Key.Contains(a)))
                        .Select(z => new KeyValuePair<string, int>(z.Key, z.Value.Sum(a => a.Value)))
                    )
                    .SelectMany(y => y)
                    .GroupBy(y => y.Key)
                    .ToDictionary(k => k.Key, v => v.Sum(z => z.Value))
                 )
            ).ToDictionary(k => k.Key, v => v.Value);


        [JsonPropertyName("referrers_total")]
        public Dictionary<string, int> ReferrersTotal =>
             Containers.SelectMany(x => x.Value.Referrers)
                .Select(y => y.Value)
                .Select(y => y
                    .Where(z => !REFERRER_BLACKLIST.Any(a => z.Key.Contains(a)))
                    .Select(z => new KeyValuePair<string, int>(z.Key, z.Value.Sum(a => a.Value)))
                )
                .SelectMany(y => y)
                .GroupBy(y => y.Key)
                .ToDictionary(k => k.Key, v => v.Sum(z => z.Value))
                .OrderByDescending(y => y.Value)
                .ToDictionary(k => k.Key, v => v.Value);

        /// <summary>
        /// Lists the ratelimit exceedances per day
        /// </summary>
        [JsonPropertyName("ratelimit_exceedances")]
        public Dictionary<string, int> RatelimitExceedances =>
            Containers.Select(x =>
                new KeyValuePair<string, int>(
                    x.Key.ToString("yyyy.MM.dd"),
                    x.Value.RatelimitExceedances.Sum(y => y.Value)
                )
            ).ToDictionary(k => k.Key, v => v.Value);


        /// <summary>
        /// Displays the API calls
        /// </summary>
        [JsonPropertyName("api_endpoint_calls")]
        public Dictionary<string, Dictionary<string, Dictionary<CacheState, int>>> ApiEndpointCalls =>
            Containers.Select(x =>
                new KeyValuePair<string, Dictionary<string, Dictionary<CacheState, int>>>(
                    x.Key.ToString("yyyy.MM.dd"),
                    x.Value.ApiEndpointCalls
                        .SelectMany(y => y.Value.Values)
                        .GroupBy(y => y.Endpoint)
                        .Select(y => new KeyValuePair<string, Dictionary<CacheState, int>>(
                            y.First().Endpoint,
                            y.GroupBy(z => z.ServedFromCache)
                             .Select(z => new KeyValuePair<CacheState, int>((z.Key ? CacheState.FromCache : CacheState.FromLive), z.Sum(a => a.Count)))
                                .ToDictionary(z => z.Key, z => z.Value)
                        )
                    ).ToDictionary(k => k.Key, v => v.Value)
                )
            ).ToDictionary(z => z.Key, z => z.Value);


        [JsonPropertyName("api_endpoint_caching")]
        public Dictionary<CacheState, int> ApiEndpointCaching =>
            ApiEndpointCalls
                .Values.SelectMany(x => x.Values)
                .Select(x =>
                    x.Select(y => y))
                .SelectMany(x => x)
                .GroupBy(x => x.Key)
                .Select(x => new KeyValuePair<CacheState, int>(x.Key, x.Sum(y => y.Value)))
                .ToDictionary(k => k.Key, v => v.Value);


        [JsonPropertyName("api_endpoints_total")]
        public Dictionary<string, int> ApiEndpointsTotal =>
            ApiEndpointCalls
                .Values.SelectMany(x => x)
                .Select(x => new KeyValuePair<string, int>(
                    x.Key,
                    x.Value.Sum(y => y.Value)
                ))
                .GroupBy(x => x.Key)
                .ToDictionary(k => k.Key, v => v.Sum(y => y.Value))
                .OrderByDescending(y => y.Value)
                .ToDictionary(k => k.Key, v => v.Value);

        /// <summary>
        /// Returns the most requested emblems
        /// </summary>
        [JsonPropertyName("requested_emblems")]
        public Dictionary<string, int> RequestedEmblems =>
           Containers.SelectMany(x => x.Value.CreatedEmblemsRequests)
                .Select(y => y.Value)
                .Select(y => y
                    .Select(z => new KeyValuePair<string, int>(z.Key, z.Value))
                )
                .SelectMany(y => y)
                .GroupBy(y => y.Key)
                .ToDictionary(k => k.Key, v => v.Sum(z => z.Value))
                .OrderByDescending(y => y.Value)
                .Take(TOPLIST_SIZE)
                .ToDictionary(k => k.Key, v => v.Value);
    }
}