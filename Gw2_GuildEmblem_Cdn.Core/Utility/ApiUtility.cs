using Gw2_GuildEmblem_Cdn.Core.Custom.Gw2SharpWebApi.Caching;
using Gw2_GuildEmblem_Cdn.Core.Custom.Gw2SharpWebApi.Middleware;
using Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.Caching;
using Gw2Sharp.WebApi.V2.Clients;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Utility
{
    public class ApiUtility : IDisposable, IApiUtility
    {
        private bool _isDisposed;
        //private static Lazy<ApiUtility> _instance = new Lazy<ApiUtility>(() => new ApiUtility());

        //public static ApiUtility Instance
        //{
        //    get => _instance.Value;
        //}

        private readonly ILogger _log;
        private readonly IConfiguration _config;

        private Gw2Sharp.Gw2Client _memoryCachedClient = null;
        private Gw2Sharp.Gw2Client _archiveCachedClient = null;

        private Gw2Sharp.Connection _memoryCachedConnection = null;
        private Gw2Sharp.Connection _archiveCachedConnection = null;
        private ArchiveCacheMethod _archiveRenderCacheMethod = null;
        private MemoryCacheMethod _memoryWebApiCacheMethod = null;
        private ArchiveCacheMethod _archiveWebApiCacheMethod = null;

        public ApiUtility(IConfiguration config, IStatisticsUtility statistics, ILogger<ApiUtility> log)
        {
            _config = config;
            _log = log;

            _archiveRenderCacheMethod = new DelayedExpiryArchiveCacheMethod(TimeSpan.FromDays(7), Path.Combine(_config["cachePath"], "render.zip"));
            _archiveWebApiCacheMethod = new DelayedExpiryArchiveCacheMethod(TimeSpan.FromDays(7), Path.Combine(_config["cachePath"], "cache.zip"));
            _memoryWebApiCacheMethod = new DelayedExpiryMemoryCacheMethod(TimeSpan.FromDays(1), 1000 /*ms*/ * 60 /*sec*/ * 60 /*min*/ * 24 /*hrs*/);

            _memoryCachedConnection = new Gw2Sharp.Connection()
            {
                CacheMethod = _memoryWebApiCacheMethod
            };
            _archiveCachedConnection = new Gw2Sharp.Connection()
            {
                CacheMethod = _archiveWebApiCacheMethod,
                RenderCacheMethod = _archiveRenderCacheMethod
            };


            _memoryCachedConnection.Middleware.Insert(0, new RateLimiterMiddleware(statistics, _log));
            _archiveCachedConnection.Middleware.Insert(0, new RateLimiterMiddleware(statistics, _log));

            _memoryCachedConnection.Middleware.Insert(0, new StatisticsMiddleware(statistics));
            _archiveCachedConnection.Middleware.Insert(0, new StatisticsMiddleware(statistics));

            _memoryCachedClient = new Gw2Sharp.Gw2Client(_memoryCachedConnection);
            _archiveCachedClient = new Gw2Sharp.Gw2Client(_archiveCachedConnection);
        }

        /// <summary>
        /// Returns a guild from the API
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public async Task<Guild> GetGuild(string guildId)
        {
            Guild guild = null;
            try
            {
                IGuildClient guildClient = _memoryCachedClient.WebApi.V2.Guild;
                guild = await guildClient[guildId].GetAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, guildId);
            }
            return guild;
        }

        /// <summary>
        /// Returns necessary information for emblem generation
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task<(Emblem, Emblem, List<Color>)> GetEmblemInformation(Guild guild)
        {
            List<Color> colors = new List<Color>();
            Emblem emblemBackground = null;
            Emblem emblemForeground = null;

            try
            {
                if (guild != null)
                {
                    emblemBackground = await _archiveCachedClient.WebApi.V2.Emblem.Backgrounds.GetAsync(guild.Emblem.Background.Id);
                    emblemForeground = await _archiveCachedClient.WebApi.V2.Emblem.Foregrounds.GetAsync(guild.Emblem.Foreground.Id);


                    List<int> colorIds = guild.Emblem.Foreground.Colors.ToList();
                    colorIds.AddRange(guild.Emblem.Background.Colors);
                    colorIds = colorIds.Distinct().ToList();

                    colors.AddRange(await _archiveCachedClient.WebApi.V2.Colors.ManyAsync(colorIds));

                    return (emblemBackground, emblemForeground, colors);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, guild.ToString());
            }

            return (null, null, null);
        }

        /// <summary>9
        /// Downloads an image from the render API
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<byte[]> GetImage(RenderUrl url)
        {
            byte[] buffer = await _archiveCachedClient.WebApi.Render.DownloadToByteArrayAsync(url.Url);
            return buffer;
        }


        /// <summary>
        /// Dispose Wrapper
        ///  -> Force the Cache Methods to close the archives via disposing them
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _archiveRenderCacheMethod.Dispose();
                _archiveWebApiCacheMethod.Dispose();
                _memoryWebApiCacheMethod.Dispose();

                _memoryCachedClient.Dispose();
                _archiveCachedClient.Dispose();

                _isDisposed = true;
            }
        }
    }
}