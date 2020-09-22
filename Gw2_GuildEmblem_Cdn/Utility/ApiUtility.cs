using Gw2_GuildEmblem_Cdn.Custom.Gw2SharpWebApi.Caching;
using Gw2_GuildEmblem_Cdn.Custom.Gw2SharpWebApi.Middleware;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.Caching;
using Gw2Sharp.WebApi.V2.Clients;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Utility
{
    public class ApiUtility : IDisposable
    {
        private bool _isDisposed;
        private static Lazy<ApiUtility> _instance = new Lazy<ApiUtility>(() => new ApiUtility());

        public static ApiUtility Instance
        {
            get => _instance.Value;
        }

        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Gw2Sharp.Gw2Client _memoryCachedClient = null;
        private Gw2Sharp.Gw2Client _archiveCachedClient = null;

        private Gw2Sharp.Connection _memoryCachedConnection = null;
        private Gw2Sharp.Connection _archiveCachedConnection = null;
        private ArchiveCacheMethod _archiveRenderCacheMethod = null;
        private MemoryCacheMethod _memoryWebApiCacheMethod = null;
        private ArchiveCacheMethod _archiveWebApiCacheMethod = null;

        private ApiUtility()
        {
            _archiveRenderCacheMethod = new DelayedExpiryArchiveCacheMethod(TimeSpan.FromDays(7), Path.Combine(ConfigurationManager.AppSettings["cache_path"], "render.zip"));
            _archiveWebApiCacheMethod = new DelayedExpiryArchiveCacheMethod(TimeSpan.FromDays(7), Path.Combine(ConfigurationManager.AppSettings["cache_path"], "cache.zip"));
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


            _memoryCachedConnection.Middleware.Insert(0, new RateLimiterMiddleware());
            _archiveCachedConnection.Middleware.Insert(0, new RateLimiterMiddleware());

            _memoryCachedConnection.Middleware.Insert(0, new StatisticsMiddleware());
            _archiveCachedConnection.Middleware.Insert(0, new StatisticsMiddleware());

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
                _log.Error(ex);
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
                _log.Error(ex);
            }

            return (null, null, null);
        }

        /// <summary>9
        /// Downloads an image from the render API
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<System.Drawing.Image> GetImage(RenderUrl url)
        {
            byte[] buffer = await _archiveCachedClient.WebApi.Render.DownloadToByteArrayAsync(url.Url);

            using (Stream stream = new MemoryStream(buffer))
            {
                return System.Drawing.Image.FromStream(stream);
            }
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