using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Gw2_GuildEmblem_Cdn.Core.Extensions
{
    public static class ImageExtensions
    {
        /// <summary>
        /// Returns an HttpResponseMessage which includes the image
        /// </summary>
        /// <param name="img"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static HttpResponseMessage ToHttpResponse(this Image img, ImageFormat format, Controllers.EmblemController.EmblemStatus emblemStatus)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, format);
                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                string mime = codecs.First(codec => codec.FormatID == format.Guid).MimeType;

                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(stream.ToArray());
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);

                result.Content.Headers.Add(Controllers.EmblemController.EMBLEM_STATUS_HEADER_KEY, emblemStatus.ToString());

                return result;
            }
        }

        public static IActionResult ToActionResult(this Image img, Controller controller, ImageFormat format, Controllers.EmblemController.EmblemStatus emblemStatus)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, format);
                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                string mime = codecs.First(codec => codec.FormatID == format.Guid).MimeType;

                byte[] buffer = stream.ToArray();
                IActionResult result = controller.File(buffer, mime);
                controller.Response.Headers.Add(Controllers.EmblemController.EMBLEM_STATUS_HEADER_KEY, emblemStatus.ToString());

                return result;
            }
        }
    }
}