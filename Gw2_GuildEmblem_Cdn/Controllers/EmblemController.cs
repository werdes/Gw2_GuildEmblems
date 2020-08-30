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
        /// Empty emblem when guild not found/guild has no emblem
        /// 500 on random exceptions
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowCrossSiteJson]
        [Route("emblem/{guildId}")]
        [OutputCache(
            MaxAge = 60 /*Seconds*/ * 60 /*Minutes*/ * 24 /*Hours*/,
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
            MaxAge = 60 /*Seconds*/ * 60 /*Minutes*/ * 24 /*Hours*/,
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
            Bitmap retImage;
            Guild guild = await ApiUtility.Instance.GetGuild(guildId);
           
            if (guild != null && guild.Emblem != null)
            {
                //Try to find in cache first
                if (!CacheUtility.Instance.TryGetEmblem(guild, size, out retImage))
                {
                    (Emblem emblemBackground, Emblem emblemForeground, List<Gw2Sharp.WebApi.V2.Models.Color> colors) = await ApiUtility.Instance.GetEmblemInformation(guild);
                    
                    retImage = await CreateEmblem(guild, emblemBackground, emblemForeground, colors, size);

                    CacheUtility.Instance.SetEmblem(guild, size, retImage);
                }

                return retImage.ToHttpResponse(ImageFormat.Png);
            }
            else
            {
                //Return the Null Emblem
                retImage = GetNullEmblem(size);
                if (retImage != null)
                    return retImage.ToHttpResponse(ImageFormat.Png);

                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
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

            if (!CacheUtility.Instance.TryGetEmblem(null, size, out retImage))
            {
                string path = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, ConfigurationManager.AppSettings["null_emblem"]);
                retImage = new Bitmap(path);
                retImage = ImageUtility.Resize(retImage, size);
                

                CacheUtility.Instance.SetEmblem(null, size, retImage);
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
                            (Bitmap)await ApiUtility.Instance.GetImage(url),
                        shadeColor),
                    transformations);

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
