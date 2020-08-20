using Gw2_GuildEmblem_Cdn.Custom.Caching;
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
        private RatelimitHandler _ratelimitHandler = null;

        private Gw2Sharp.Gw2Client _cachedClient = null;

        private Gw2Sharp.Connection _cachedConnection = null;
        private ArchiveCacheMethod _renderCacheArchiveMethod = null;
        private MemoryCacheMethod _memoryCacheMethod = null;

        private ApiUtility()
        {
            _ratelimitHandler = new RatelimitHandler(100, nameof(ApiUtility));
            _renderCacheArchiveMethod = new DelayedExpiryArchiveCacheMethod(TimeSpan.FromDays(1), Path.Combine(ConfigurationManager.AppSettings["cache_path"], "render.zip"));
            _memoryCacheMethod = new DelayedExpiryMemoryCacheMethod(TimeSpan.FromDays(1), 1000 /*ms*/ * 60 /*sec*/ * 60 /*min*/ * 24 /*hrs*/);

            _cachedConnection = new Gw2Sharp.Connection()
            {
                CacheMethod = _memoryCacheMethod,
                RenderCacheMethod = _renderCacheArchiveMethod
            };

            _cachedClient = new Gw2Sharp.Gw2Client(_cachedConnection);
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
                _ratelimitHandler.Wait();

                IGuildClient guildClient = _cachedClient.WebApi.V2.Guild;
                guild = await guildClient[guildId].GetAsync();

                _ratelimitHandler.Set(guild);
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
                    _ratelimitHandler.Wait();
                    emblemBackground = await _cachedClient.WebApi.V2.Emblem.Backgrounds.GetAsync(guild.Emblem.Background.Id);
                    _ratelimitHandler.Set(emblemBackground);


                    _ratelimitHandler.Wait();
                    emblemForeground = await _cachedClient.WebApi.V2.Emblem.Foregrounds.GetAsync(guild.Emblem.Foreground.Id);
                    _ratelimitHandler.Set(emblemForeground);


                    List<int> colorIds = guild.Emblem.Foreground.Colors.ToList();
                    colorIds.AddRange(guild.Emblem.Background.Colors);
                    colorIds = colorIds.Distinct().ToList();

                    _ratelimitHandler.Wait();
                    colors.AddRange(await _cachedClient.WebApi.V2.Colors.ManyAsync(colorIds));
                    _ratelimitHandler.Set(colors);

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
            byte[] buffer = await _cachedClient.WebApi.Render.DownloadToByteArrayAsync(url.Url);

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
                _renderCacheArchiveMethod.Dispose();
                _memoryCacheMethod.Dispose();

                _cachedClient.Dispose();

                _isDisposed = true;
            }
        }
    }
}