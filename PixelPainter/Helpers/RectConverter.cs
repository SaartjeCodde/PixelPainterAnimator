using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PixelPainter.Model;
using Color = System.Drawing.Color;

namespace PixelPainter.Helpers
{
    internal static class RectConverter
    {
        public static Bitmap RectToBitmap(List<RectItem> rects, int widthMap, int heightMap)
        {
            Bitmap lastImage = null;

            lastImage = new Bitmap(widthMap * 10, heightMap * 10);
            using (Graphics gfx = Graphics.FromImage(lastImage))
            {
                gfx.Clear(Color.Transparent);
                int xOffset = 0;
                int yOffset = 0;
                foreach (var rect in rects)
                {
                    var bitmap = CreateBitmap(10, 10, rect.PixelColor.Color);
                    gfx.DrawImage(bitmap, new Rectangle(xOffset, yOffset, 10, 10));
                    xOffset += 10;
                    if (xOffset != widthMap * 10) continue;
                    xOffset = 0;
                    yOffset += 10;
                }
            }
            return lastImage;
        }

        private static Bitmap CreateBitmap(int width, int height, System.Windows.Media.Color color)
        {
            var pixelColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            Bitmap bitmap = new Bitmap(width, height);
            for (int x = 0; x < bitmap.Height; ++x)
            {
                for (int y = 0; y < bitmap.Width; ++y)
                {
                    bitmap.SetPixel(x, y, pixelColor);
                }
            }
            for (int x = 0; x < bitmap.Height; ++x)
            {
                bitmap.SetPixel(x, x, pixelColor);
            }
            return bitmap;
        }
       
        public static Bitmap CombineBitmaps(List<Bitmap> bitmapList)
        {
            int currentSprite = 0;
            Bitmap spriteSheet = null;
            int width = bitmapList.ElementAt(0).Width;
            int height = bitmapList.ElementAt(0).Height;
            double rows = bitmapList.Count;
            double spriteSheetHeight = height;
            double spriteSheetWidth = rows * width;
            spriteSheet = new Bitmap((int)spriteSheetWidth, (int)spriteSheetHeight);
            foreach (var bmp in bitmapList)
            {
                using (Graphics gfx = Graphics.FromImage(spriteSheet))
                {
                    double bmpY = 0;
                    double bmpX = (currentSprite) * width;
                    gfx.DrawImage(bmp, new Rectangle((int)bmpX, (int)bmpY, width, height));
                }
                currentSprite++;
            }
            return spriteSheet;
        }
    }
}

