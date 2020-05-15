using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gw2_GuildEmblem_Cdn.Configuration;
using Flatwhite.WebApi;
using System.Web.Http;
using System.Net.Http;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Drawing.Drawing2D;
using Gw2_GuildEmblem_Cdn.Extensions;
using System.Drawing.Imaging;
using Gw2Sharp.WebApi.V2.Models;
using Gw2_GuildEmblem_Cdn.Utility;
using System.Threading.Tasks;
using Gw2Sharp.WebApi;
using System.Configuration;

namespace Gw2_GuildEmblem_Cdn.Controllers
{
    public class EmblemController : ApiController
    {
        private const int DEFAULT_IMAGE_SIZE = 128;
        private const int MIN_IMAGE_SIZE = 1;
        private const int MAX_IMAGE_SIZE = 512;

        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        /// <summary>
        /// Returns the Guild emblem of said guild-id in 128x128px
        /// 404 if the guild cannot be found via API
        /// 500 on random exceptions
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowCrossSiteJson]
        [Route("emblem/{guildId}")]
        [OutputCache(
            MaxAge = 60 * 60 * 24, //24 Hours
            StaleWhileRevalidate = 5,
            VaryByParam = "*",
            IgnoreRevalidationRequest = true)]
        public async Task<HttpResponseMessage> Get(string guildId)
        {
            try
            {
                return await GetInternal(guildId, DEFAULT_IMAGE_SIZE);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Returns a resized version of said image
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowCrossSiteJson]
        [Route("emblem/{guildId}/{size}")]
        [OutputCache(
            MaxAge = 60 * 60 * 24, //24 Hours
            StaleWhileRevalidate = 5,
            VaryByParam = "*",
            IgnoreRevalidationRequest = true)]
        public async Task<HttpResponseMessage> GetResized(string guildId, int size)
        {
            try
            {
                //confine sizes
                size = Math.Min(Math.Max(MIN_IMAGE_SIZE, size), MAX_IMAGE_SIZE);
                return await GetInternal(guildId, size);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Returns the emblem, either cached or freshly created
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> GetInternal(string guildId, int size)
        {
            (Guild guild, Emblem emblemBackground, Emblem emblemForeground, List<Gw2Sharp.WebApi.V2.Models.Color> colors) = await GuildUtility.Instance.GetGuildInformation(guildId);

            if (guild != null)
            {
                Bitmap retImage;

                //Try to find in cache first
                if (!CacheUtility.Instance.TryGet(guild, size, out retImage))
                {
                    retImage = await CreateEmblem(guild, emblemBackground, emblemForeground, colors, size);

                    CacheUtility.Instance.Set(guild, size, retImage);
                }

                return retImage.ToHttpResponse(ImageFormat.Png);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
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
                        List<RotateFlipType> flipTypes = new List<RotateFlipType>();
                        if (guild.Emblem.Flags.Any(x => x.Value == GuildEmblemFlag.FlipBackgroundHorizontal))
                            flipTypes.Add(RotateFlipType.RotateNoneFlipX);
                        if (guild.Emblem.Flags.Any(x => x.Value == GuildEmblemFlag.FlipBackgroundVertical))
                            flipTypes.Add(RotateFlipType.RotateNoneFlipY);

                        lstLayers.Add((foregrounds[i], System.Drawing.Color.FromArgb(color.Cloth.Rgb[0], color.Cloth.Rgb[1], color.Cloth.Rgb[2]), flipTypes));
                    }
                }
            }

            // 1.) Download Images
            // 2.) Shade according to color
            // 3.) Apply Rotations/Flips
            // 4.) Resize if necessary
            List<Bitmap> layers = new List<Bitmap>();
            foreach ((RenderUrl url, System.Drawing.Color shadeColor, List<RotateFlipType> rotations) in lstLayers)
            {
                Bitmap layer =
                    ImageUtility.ApplyRotations(
                        ImageUtility.ShadeImageFromAlpha(
                            (Bitmap)await GuildUtility.Instance.GetImage(url),
                        shadeColor),
                    rotations);

                if (size != DEFAULT_IMAGE_SIZE)
                {
                    layer = ImageUtility.Resize(layer, size);
                }

                layers.Add(layer);
            }

            //Merge layers
            Bitmap retImage = ImageUtility.LayerImages(layers);
            return retImage;
        }
    }
}
