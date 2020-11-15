using Gw2_GuildEmblem_Cdn.Core.Configuration;
using Gw2_GuildEmblem_Cdn.Core.Custom;
using Gw2_GuildEmblem_Cdn.Core.Extensions;
using Gw2_GuildEmblem_Cdn.Core.Utility;
using Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Controllers
{
    public class EmblemController : Controller
    {
        public const int DEFAULT_IMAGE_SIZE = 128;
        private const int MIN_IMAGE_SIZE = 1;
        private const int MAX_IMAGE_SIZE = 512;
        public const string EMBLEM_STATUS_HEADER_KEY = "Emblem-Status";

        private readonly ILogger _log = Log.Factory.CreateLogger<EmblemController>();
        private readonly IConfiguration _config;
        private readonly IEmblemCacheUtility _cache;
        private readonly IApiUtility _api;
        private readonly IStatisticsUtility _statistics;

        public enum EmblemStatus
        {
            OK,
            NotFound
        }

        public EmblemController(IConfiguration config, IEmblemCacheUtility cache, IApiUtility api, IStatisticsUtility statistics)
        {
            _config = config;
            _cache = cache;
            _api = api;
            _statistics = statistics;
        }

        /// <summary>
        /// Returns the Guild emblem of said guild-id in 128x128px
        /// Empty emblem when guild not found/guild has no emblem
        /// 500 on random exceptions
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableCors(Startup.AllowSpecificOriginsName)]
        [Route("emblem/{guildId}")]
        [ResponseCache(Duration = 60 /*Seconds*/ * 60 /*Minutes*/ * 24 /*Hours*/)]
        [LogStatistics]
        public async Task<IActionResult> Get(string guildId)
        {
            try
            {

                //Register for Request (If it comes through to here, it's not cached)
                _statistics.RegisterResponseAsync(ControllerContext.HttpContext.Request, false);
                _statistics.RegisterReferrerAsync(ControllerContext.HttpContext.Request, false);

                return await GetInternal(guildId, DEFAULT_IMAGE_SIZE);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, guildId);
            }

            return StatusCode(500);
        }

        /// <summary>
        /// Returns a resized version of said image
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [System.Web.Http.HttpGet]
        [EnableCors(Startup.AllowSpecificOriginsName)]
        [System.Web.Http.Route("emblem/{guildId}/{size}")]
        [ResponseCache(Duration = 60 /*Seconds*/ * 60 /*Minutes*/ * 24 /*Hours*/)]
        [LogStatistics]
        public async Task<IActionResult> GetResized(string guildId, int size)
        {
            try
            {
                //Register for Request (If it comes through to here, it's not cached)
                _statistics.RegisterResponseAsync(ControllerContext.HttpContext.Request, false);
                _statistics.RegisterReferrerAsync(ControllerContext.HttpContext.Request, false);

                //confine sizes
                size = Math.Min(Math.Max(MIN_IMAGE_SIZE, size), MAX_IMAGE_SIZE);
                return await GetInternal(guildId, size);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, guildId);
            }
            return StatusCode(500);
        }

        /// <summary>
        /// Returns an emblem by its descriptor string
        ///     Will not create any emblems, 404 if the emblem hasn't been created / the descriptor string doesn't match
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        [HttpGet]
        [EnableCors(Startup.AllowSpecificOriginsName)]
        [Route("emblem/raw/{descriptor}")]
        [ResponseCache(Duration = 60 /*Seconds*/ * 60 /*Minutes*/ * 24 /*Hours*/)]
        public HttpResponseMessage GetRaw(string descriptor)
        {
            try
            {
                if (EmblemCacheUtility.CACHE_NAME_VALIDATOR.IsMatch(descriptor))
                {
                    Bitmap emblem = null;
                    if (_cache.TryGetRaw(descriptor, out emblem))
                    {
                        return emblem.ToHttpResponse(ImageFormat.Png, EmblemStatus.OK);
                    }
                    else return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                else return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, descriptor);
            }
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Returns the emblem, either cached or freshly created
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private async Task<IActionResult> GetInternal(string guildId, int size)
        {
            Bitmap retImage;
            Guild guild = await _api.GetGuild(guildId);

            if (guild != null && guild.Emblem != null)
            {
                //Try to find in cache first
                if (!_cache.TryGetEmblem(guild, size, out retImage))
                {
                    (Emblem emblemBackground, Emblem emblemForeground, List<Gw2Sharp.WebApi.V2.Models.Color> colors) = await _api.GetEmblemInformation(guild);

                    retImage = await CreateEmblem(guild, emblemBackground, emblemForeground, colors, size);

                    _cache.SetEmblem(guild, size, retImage);
                }

                return retImage.ToActionResult(this, ImageFormat.Png, EmblemStatus.OK);
            }
            else
            {
                //Return the Null Emblem
                retImage = GetNullEmblem(size);
                if (retImage != null)
                    return retImage.ToActionResult(this, ImageFormat.Png, EmblemStatus.NotFound);

                return StatusCode(500);
            }
        }


        /// <summary>
        /// Returns a resized version of the Null Emblem
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private Bitmap GetNullEmblem(int size)
        {
            Bitmap retImage = null;

            if (!_cache.TryGetEmblem(null, size, out retImage))
            {
                string path = _config["nullEmblem"];
                retImage = new Bitmap(path);
                retImage = ImageUtility.Resize(retImage, size);


                _cache.SetEmblem(null, size, retImage);
            }

            return retImage;
        }

        /// <summary>
        /// Creates the emblem from API information
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="emblemBackground"></param>
        /// <param name="emblemForeground"></param>
        /// <param name="colors"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private async Task<Bitmap> CreateEmblem(Guild guild, Emblem emblemBackground, Emblem emblemForeground, List<Gw2Sharp.WebApi.V2.Models.Color> colors, int size)
        {
            //Layer back- and foreground
            RenderUrl[] backgrounds = emblemBackground.Layers.ToArray();
            RenderUrl[] foregrounds = emblemForeground.Layers.Skip(1).Select(x => x).ToArray(); //First image in foreground layers is not used - it's some weird red version 

            List<(RenderUrl, System.Drawing.Color, List<RotateFlipType>)> lstLayers = new List<(RenderUrl, System.Drawing.Color, List<RotateFlipType>)>();

            //Put together background-images with url, color and rotation instructions
            for (int i = 0; i < backgrounds.Length; i++)
            {
                int colorId;

                if (guild.Emblem.Background.Colors.TryGet(i, out colorId))
                {
                    Gw2Sharp.WebApi.V2.Models.Color color = colors.Where(x => x.Id == colorId).FirstOrDefault();
                    List<RotateFlipType> flipTypes = new List<RotateFlipType>();
                    if (color != null)
                    {
                        if (guild.Emblem.Flags.Any(x => x.Value == GuildEmblemFlag.FlipBackgroundHorizontal))
                            flipTypes.Add(RotateFlipType.RotateNoneFlipX);
                        if (guild.Emblem.Flags.Any(x => x.Value == GuildEmblemFlag.FlipBackgroundVertical))
                            flipTypes.Add(RotateFlipType.RotateNoneFlipY);

                        lstLayers.Add((backgrounds[i], System.Drawing.Color.FromArgb(color.Cloth.Rgb[0], color.Cloth.Rgb[1], color.Cloth.Rgb[2]), flipTypes));
                    }
                }
            }


            //Put together foreground-images with url, color and rotation instructions
            for (int i = 0; i < foregrounds.Length; i++)
            {
                int colorId;

                if (guild.Emblem.Foreground.Colors.TryGet(i, out colorId))
                {
                    Gw2Sharp.WebApi.V2.Models.Color color = colors.Where(x => x.Id == colorId).FirstOrDefault();

                    if (color != null)
                    {
                        List<RotateFlipType> transformations = new List<RotateFlipType>();
                        if (guild.Emblem.Flags.Any(x => x.Value == GuildEmblemFlag.FlipForegroundHorizontal))
                            transformations.Add(RotateFlipType.RotateNoneFlipX);
                        if (guild.Emblem.Flags.Any(x => x.Value == GuildEmblemFlag.FlipForegroundVertical))
                            transformations.Add(RotateFlipType.RotateNoneFlipY);

                        lstLayers.Add((foregrounds[i], System.Drawing.Color.FromArgb(color.Cloth.Rgb[0], color.Cloth.Rgb[1], color.Cloth.Rgb[2]), transformations));
                    }
                }
            }

            // 1.) Download Images
            // 2.) Shade according to color
            // 3.) Apply Rotations/Flips
            // 4.) Resize if necessary
            List<Bitmap> layers = new List<Bitmap>();
            foreach ((RenderUrl url, System.Drawing.Color shadeColor, List<RotateFlipType> transformations) in lstLayers)
            {
                Bitmap layer =
                    ImageUtility.ApplyRotations(
                        ImageUtility.ShadeImageFromAlpha(
                            (Bitmap)await _api.GetImage(url),
                        shadeColor),
                    transformations);

                if (size != DEFAULT_IMAGE_SIZE)
                {
                    layer = ImageUtility.Resize(layer, size);
                }

                layers.Add(layer);
            }

            //Register for statistics purposes
            _statistics.RegisterCreationAsync(guild, size);

            //Merge layers
            Bitmap retImage = ImageUtility.LayerImages(layers);
            return retImage;
        }
    }
}
