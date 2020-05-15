using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Utility
{
    public class ImageUtility
    {
        /// <summary>
        /// shades an image in a certain color defined by the images alpha values
        /// </summary>
        /// <param name="image"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Bitmap ShadeImageFromAlpha(Bitmap image, Color color)
        {
            Bitmap retImage = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
                for (int y = 0; y < image.Height; y++)
                {
                    byte alpha = image.GetPixel(x, y).A;
                    Color newColor = Color.FromArgb(alpha, color);
                    retImage.SetPixel(x, y, newColor);
                }

            return retImage;
        }

        /// <summary>
        /// Layers a set of images 
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        public static Bitmap LayerImages(IEnumerable<Bitmap> images)
        {
            Bitmap retImage = new Bitmap(images.First().Width, images.First().Height);
            using (var g = Graphics.FromImage(retImage))
            {
                foreach (Image image in images)
                {
                    g.DrawImage(image, 0, 0);
                }
            }

            return retImage;
        }

        /// <summary>
        /// Applies a set of rotations to an image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="rotations"></param>
        /// <returns></returns>
        public static Bitmap ApplyRotations(Bitmap image, IEnumerable<RotateFlipType> rotations)
        {
            foreach (RotateFlipType rotation in rotations)
            {
                image.RotateFlip(rotation);
            }

            return image;
        }

        /// <summary>
        /// Resizes an image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Bitmap Resize(Bitmap image, int size)
        {
            return new Bitmap(image, size, size);
        }
    }
}