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

namespace Gw2_GuildEmblem_Cdn.Controllers
{
    public class EmblemController : ApiController
    {
        [HttpGet]
        [AllowCrossSiteJson]
        [Route("emblem/cachetest")]
        [OutputCache(
            MaxAge = 10, //Seconds
            StaleWhileRevalidate = 5,
            VaryByParam = "*",
            IgnoreRevalidationRequest = true)]
        public HttpResponseMessage Get()
        {
            Random rnd = new Random();

            Bitmap img = new Bitmap(256, 256);
            for (int i = 0; i < img.Height; i++)
                for (int j = 0; j < 50; j++)
                     img.SetPixel(i, j, Color.White);

            for (int i = 0; i < img.Height; i++)
                for (int j = 50; j < img.Width; j++)
                    img.SetPixel(i, j, Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
            Graphics graphics = Graphics.FromImage(img);
            RectangleF rectangle = new RectangleF(5, 5, 245, 40);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawString(DateTime.Now.ToString("s"), new Font("Arial", 8), Brushes.Black, rectangle);

            graphics.Flush();


            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(ms.ToArray());
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                return result;
            }
        }
    }
}
