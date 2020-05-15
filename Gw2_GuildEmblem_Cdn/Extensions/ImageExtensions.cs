using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Extensions
{
    public static class ImageExtensions
    {
        /// <summary>
        /// Returns an HttpResponseMessage which includes the image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static HttpResponseMessage ToHttpResponse(this Image img, ImageFormat format)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, format);
                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                string mime = codecs.First(codec => codec.FormatID == format.Guid).MimeType;

                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(stream.ToArray());
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);

                return result;
            }
        }
    }
}