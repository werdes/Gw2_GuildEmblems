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
using System.Text.RegularExpressions;
using Gw2_GuildEmblem_Cdn.Extensions;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Utility
{
    public class CacheUtility
    {
        public const string EMBLEM_CACHE_DIRECTORY_NAME = "emblem";
        public const string EMBLEM_CACHE_EXTENSION = ".png"; 
        public readonly static Regex CACHE_NAME_VALIDATOR = new Regex("(([0-9]{1,3})-([0-9.]+)_([0-9]{1,3})-([0-9.]*)(_){0,1}([\"FlipBackgroundHorizontal\"|\"FlipBackgroundVertical\"|\"FlipForegroundHorizontal\"|\"FlipForegroundVertical\"|.])*_([0-9]{1,3}))|((null_)([0-9]{1,3}))", RegexOptions.Compiled);

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
        /// Returns the amount of created Emblems
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCountEmblemsInCache()
        {
            return await Task.Run(delegate
            {
                DirectoryInfo directory = new DirectoryInfo(Path.Combine(ConfigurationManager.AppSettings["cache_path"], EMBLEM_CACHE_DIRECTORY_NAME));
                return directory.GetFiles().Length;
            });
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
            string descriptor = GetEmblemDescriptor(guild, size);
            string filePath = Path.Combine(ConfigurationManager.AppSettings["cache_path"], EMBLEM_CACHE_DIRECTORY_NAME, descriptor + EMBLEM_CACHE_EXTENSION);

            StatisticsUtility.Instance.RegisterEmblemRequestAsync(descriptor);

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
        /// Gets an emblem by its description string
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        public bool TryGetRaw(string descriptor, out Bitmap retVal)
        {
            string filePath = Path.Combine(ConfigurationManager.AppSettings["cache_path"], EMBLEM_CACHE_DIRECTORY_NAME, $"{descriptor}.png");
            if (System.IO.File.Exists(filePath))
            {
                retVal = new Bitmap(filePath);
                return true;
            }
            else
            {
                retVal = null;
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
            string filePath = Path.Combine(ConfigurationManager.AppSettings["cache_path"], EMBLEM_CACHE_DIRECTORY_NAME, GetEmblemDescriptor(guild, size));
            image.Save(filePath);
        }

        /// <summary>
        /// Returns a filename depending on the guild emblem parameters and the size
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private string GetEmblemDescriptor(Guild guild, int size)
        {
            if (guild != null && guild.Emblem != null)
            {
                return guild.Emblem.ToDescriptorString(size);
            }
            return $"null_{size}";
        }
    }
}