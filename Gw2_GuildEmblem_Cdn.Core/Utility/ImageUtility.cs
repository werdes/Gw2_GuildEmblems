using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Gw2_GuildEmblem_Cdn.Core.Utility
{
    public class ImageUtility
    {
        public enum RotateFlipType
        {
            FlipX,
            FlipY
        }

        /// <summary>
        /// shades an image in a certain color defined by the images alpha values
        /// </summary>
        /// <param name="image"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static SKBitmap ShadeImageFromAlpha(SKBitmap image, SKColor color)
        {
            SKBitmap retImage = new SKBitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    byte alpha = image.GetPixel(x, y).Alpha;
                    SKColor newColor = new SKColor(color.Red, color.Green, color.Blue, alpha);
                    retImage.SetPixel(x, y, newColor);
                }

            return retImage;
        }

        public static SKBitmap GetSKBitmap(byte[] image)
        {
            return SKBitmap.Decode(image);
        }

        /// <summary>
        /// Layers a set of images 
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public static SKBitmap LayerImages(IEnumerable<SKBitmap> images, int size)
        {
            using (SKSurface surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                foreach (SKBitmap image in images)
                {
                    canvas.DrawBitmap(image, SKRect.Create(0, 0, size, size));
                }

                return SKBitmap.FromImage(surface.Snapshot());
            }
        }

        /// <summary>
        /// Applies a set of rotations to an image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="rotations"></param>
        /// <returns></returns>
        public static SKBitmap ApplyRotations(SKBitmap image, IEnumerable<RotateFlipType> rotations)
        {

            foreach (RotateFlipType rotation in rotations)
            {
                image = FlipImage(image, rotation);
            }

            return image;
        }

        private static SKBitmap FlipImage(SKBitmap image, RotateFlipType rotation)
        {
            SKSurface rotated = SKSurface.Create(new SKImageInfo(image.Width, image.Height));

            using (SKCanvas canvas = rotated.Canvas)
            {
                switch (rotation)
                {
                    case RotateFlipType.FlipX:
                        canvas.Scale(-1, 1);
                        canvas.Translate(-image.Width, 0);
                        break;
                    case RotateFlipType.FlipY:
                        canvas.Scale(1, -1);
                        canvas.Translate(0, -image.Height);
                        break;
                }
                canvas.DrawBitmap(image, 0, 0);
            }

            return SKBitmap.FromImage(rotated.Snapshot());
        }

        /// <summary>
        /// Resizes an image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static SKBitmap Resize(SKBitmap image, int size)
        {
            return image.Resize(new SKImageInfo(size, size), SKFilterQuality.High);
        }
    }
}