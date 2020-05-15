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
    public class GuildUtility
    {
        private static Lazy<GuildUtility> _instance = new Lazy<GuildUtility>(() => new GuildUtility());
        public static GuildUtility Instance
        {
            get => _instance.Value;
        }

        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private RatelimitHandler _ratelimitHandler = null;
        private Gw2Sharp.Gw2Client _client = null;


        private GuildUtility()
        {
            _ratelimitHandler = new RatelimitHandler(100, nameof(GuildUtility));
            _client = new Gw2Sharp.Gw2Client(new Gw2Sharp.Connection()
            {
                //RenderCacheMethod = new ArchiveCacheMethod(Path.Combine(ConfigurationManager.AppSettings["cache_path"], "renderCache.zip"))
            });
        }

        public async Task<(Guild, Emblem, Emblem, List<Color>)> GetGuildInformation(string guildId)
        {
            List<Color> colors = new List<Color>();
            try
            {
                _ratelimitHandler.Wait();

                IGuildClient guildClient = _client.WebApi.V2.Guild;
                Guild guild = await guildClient[guildId].GetAsync();
                _ratelimitHandler.Set();

                if (guild != null)
                {
                    _ratelimitHandler.Wait();
                    Emblem emblemBackground = await _client.WebApi.V2.Emblem.Backgrounds.GetAsync(guild.Emblem.Background.Id);
                    _ratelimitHandler.Set();

                    _ratelimitHandler.Wait();
                    Emblem emblemForeground = await _client.WebApi.V2.Emblem.Foregrounds.GetAsync(guild.Emblem.Foreground.Id);
                    _ratelimitHandler.Set();

                    List<int> colorIds = guild.Emblem.Foreground.Colors.ToList();
                    colorIds.AddRange(guild.Emblem.Background.Colors);
                    colorIds = colorIds.Distinct().ToList();


                    foreach (int colorId in colorIds)
                    {
                        _ratelimitHandler.Wait();
                        colors.Add(await _client.WebApi.V2.Colors.GetAsync(colorId));
                        _ratelimitHandler.Set();
                    }

                    return (guild, emblemBackground, emblemForeground, colors);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            return (null, null, null, null);
        }

        public async Task<System.Drawing.Image> GetImage(RenderUrl url)
        {
            _ratelimitHandler.Wait();
            byte[] buffer = await url.DownloadToByteArrayAsync();
            _ratelimitHandler.Set();

            using (Stream stream = new MemoryStream(buffer))
            {
                return System.Drawing.Image.FromStream(stream);
            }
        }

    }
}