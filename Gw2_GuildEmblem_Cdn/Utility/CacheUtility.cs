using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace Gw2_GuildEmblem_Cdn.Utility
{
    public class CacheUtility
    {
        private const string EMBLEM_CACHE_PATH = "emblem";

        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Lazy<CacheUtility> _instance = new Lazy<CacheUtility>(() => new CacheUtility());
        public static CacheUtility Instance
        {
            get => _instance.Value;
        }

        public CacheUtility()
        {

        }

        /// <summary>
        /// Tries to get an emblem from cache
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="size"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        public bool TryGetEmblem(Guild guild, int size, out Bitmap retVal)
        {
            string filePath = Path.Combine(ConfigurationManager.AppSettings["cache_path"], EMBLEM_CACHE_PATH, GetEmblemFilename(guild, size));
            if(System.IO.File.Exists(filePath))
            {
                retVal = new Bitmap(filePath);
                return true;
            }
            else
            {
                retVal = new Bitmap(128, 128);
                return false;
            }
        }

        /// <summary>
        /// Set an image to cache
        /// (it's just saving with a specified filename)
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="size"></param>
        /// <param name="image"></param>
        public void SetEmblem(Guild guild, int size, Bitmap image)
        {
            string filePath = Path.Combine(ConfigurationManager.AppSettings["cache_path"], EMBLEM_CACHE_PATH, GetEmblemFilename(guild, size));
            image.Save(filePath);
        }

        /// <summary>
        /// Returns a filename depending on the guild emblem parameters and the size
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private string GetEmblemFilename(Guild guild, int size)
        {
            if (guild != null && guild.Emblem != null)
            {
                string flags = guild.Emblem.Flags.Count() > 0 ? $"_{string.Join(".", guild.Emblem.Flags.Select(x => x.Value))}" : string.Empty;
                return $"{guild.Emblem.Background.Id}-{string.Join(".", guild.Emblem.Background.Colors)}_{guild.Emblem.Foreground.Id}-{string.Join(".", guild.Emblem.Foreground.Colors)}{flags}_{size}.png";
            }
            return $"null_{size}.png";
        }
    }
}