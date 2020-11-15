using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Gw2_GuildEmblem_Cdn.Core.Extensions
{
    public static class SKBitmapExtensions
    {
        public static IActionResult ToActionResult(this SKBitmap img, Controller controller, SKEncodedImageFormat format, Controllers.EmblemController.EmblemStatus emblemStatus, string descriptor)
        {
            using (SKData data = img.Encode(format, 95))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    data.SaveTo(stream);
                    stream.Close();

                    ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                    string mime = format.GetMime();

                    byte[] buffer = stream.ToArray();
                    IActionResult result = controller.File(buffer, mime);
                    controller.Response.Headers.Add(Controllers.EmblemController.EMBLEM_STATUS_HEADER_KEY, emblemStatus.ToString());
                    controller.Response.Headers.Add(Controllers.EmblemController.EMBLEM_DESCRIPTOR_HEADER_KEY, descriptor);
                    return result;
                }

            }
        }

        public static string GetMime(this SKEncodedImageFormat format)
        {
            return format switch
            {
                SKEncodedImageFormat.Bmp => "image/bmp",
                SKEncodedImageFormat.Gif => "image/gif",
                SKEncodedImageFormat.Heif => "image/heif",
                SKEncodedImageFormat.Ico => "image/x-icon",
                SKEncodedImageFormat.Jpeg => "image/jpeg",
                SKEncodedImageFormat.Ktx => "image/ktx",
                SKEncodedImageFormat.Png => "image/png",
                SKEncodedImageFormat.Wbmp => "image/vnd.wap.wbmp",
                SKEncodedImageFormat.Webp => "image/webp",
                _ => string.Empty
            };
        }
    }
}
