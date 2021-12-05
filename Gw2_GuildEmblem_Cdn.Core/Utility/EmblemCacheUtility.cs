using Gw2_GuildEmblem_Cdn.Core.Extensions;
using Gw2_GuildEmblem_Cdn.Core.Model;
using Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Utility
{
    public class EmblemCacheUtility : IEmblemCacheUtility
    {
        public const string EMBLEM_CACHE_DIRECTORY_NAME = "emblem";
        public const string EMBLEM_CACHE_EXTENSION = ".png";
        public readonly static Regex CACHE_NAME_VALIDATOR = new Regex("(([0-9]{1,3})-([0-9.]+)_([0-9]{1,3})-([0-9.]*)(_){0,1}([FlipBackgroundHorizontal|FlipBackgroundVertical|FlipForegroundHorizontal|FlipForegroundVertical|.])*(_){0,1}([BackgroundMaximizeAlpha|ForegroundMaximizeAlpha|.])*_([0-9]{1,3}))|((null_)([0-9]{1,3}))", RegexOptions.Compiled);

        private readonly IConfiguration _config;
        private readonly IStatisticsUtility _statistics;
        private readonly ILogger _log;


        public EmblemCacheUtility(IConfiguration config, IStatisticsUtility statistics, ILogger<EmblemCacheUtility> log)
        {
            _config = config;
            _statistics = statistics;
            _log = log;
        }

        /// <summary>
        /// Returns the amount of created Emblems
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCountEmblemsInCache()
        {
            return await Task.Run(delegate
            {
                DirectoryInfo directory = new DirectoryInfo(Path.Combine(_config["cachePath"], EMBLEM_CACHE_DIRECTORY_NAME));
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
        public bool TryGetEmblem(Guild guild, int size, List<ManipulationOption> lstOptions, out SKBitmap retVal)
        {
            string descriptor = GetEmblemDescriptor(guild, size, lstOptions);
            string filePath = Path.Combine(_config["cachePath"], EMBLEM_CACHE_DIRECTORY_NAME, descriptor + EMBLEM_CACHE_EXTENSION);

            _statistics.RegisterEmblemRequestAsync(descriptor);

            if (System.IO.File.Exists(filePath))
            {
                retVal =  SKBitmap.Decode(filePath);
                return true;
            }
            else
            {
                retVal = new SKBitmap(128, 128);
                return false;
            }
        }

        /// <summary>
        /// Gets an emblem by its description string
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        public bool TryGetRaw(string descriptor, out SKBitmap retVal)
        {
            string filePath = Path.Combine(_config["cachePath"], EMBLEM_CACHE_DIRECTORY_NAME, descriptor + EMBLEM_CACHE_EXTENSION);
            if (System.IO.File.Exists(filePath))
            {
                retVal = SKBitmap.Decode(filePath);
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
        public void SetEmblem(Guild guild, int size, List<ManipulationOption> lstOptions, SKBitmap image)
        {
            string descriptor = GetEmblemDescriptor(guild, size, lstOptions);
            string filePath = Path.Combine(_config["cachePath"], EMBLEM_CACHE_DIRECTORY_NAME, descriptor + EMBLEM_CACHE_EXTENSION);

            using(SKData data = image.Encode(SKEncodedImageFormat.Png, 95))
            {
                using(FileStream stream = System.IO.File.OpenWrite(filePath))
                {
                    data.SaveTo(stream);
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Returns a filename depending on the guild emblem parameters and the size
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public string GetEmblemDescriptor(Guild guild, int size, List<ManipulationOption> lstOptions)
        {
            if (guild != null && guild.Emblem != null)
            {
                return guild.Emblem.ToDescriptorString(size, lstOptions);
            }
            return $"null_{size}";
        }
    }
}