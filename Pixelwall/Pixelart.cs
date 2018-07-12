using System;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Pixelwall
{
    public enum BlockOrientation
    {
        TOP, BOTTOM, VERTICAL
    }

    public class Pixelart
    {
        public readonly int width;
        public readonly int height;
        public readonly bool dithered;
        public readonly BlockOrientation orientation;
        public Dictionary<string, int> blockUses = new Dictionary<string, int>();

        private readonly Bitmap bitmap;
        private readonly Data data;
        List<Texture> field = new List<Texture>();

        public Pixelart(Data data, int w, int h, Uri imagePath, bool dithering, BlockOrientation orientation)
        {
            width = w;
            height = h;
            string path = imagePath.LocalPath;
            bitmap = new Bitmap(path);
            bitmap = ResizeImage(bitmap, width, height);
            dithered = dithering;
            this.data = data;
            this.orientation = orientation;
            Generate();
        }

        public Pixelart(Data data, Uri imagePath, bool dithering, BlockOrientation orientation)
        {
            bitmap = new Bitmap(imagePath.AbsolutePath);
            width = bitmap.Width;
            height = bitmap.Height;
            dithered = dithering;
            this.data = data;
            this.orientation = orientation;
            Generate();
        }

        private Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void Generate()
        {
            field.Capacity = width * height;
            /*
            foreach (Block block in Data.blocks)
            {
                block.uses = 0;
            }*/

            if (dithered)
            {
                GenerateDithered();
            }
            else
            {
                GenerateNormal();
            }

            CountUses();
            MergeBlockUses();
        }

        //Counts texture uses.
        private void CountUses()
        {
            foreach (Texture text in field)
            {
                if (!blockUses.ContainsKey(text.id))
                    blockUses.Add(text.id, 0);
                blockUses[text.id]++;
            }
        }

        //Called after CountUses. Merges textures numbers that belong to single block into one entry. Number used as a key.
        private void MergeBlockUses()
        {
            for (int i = 0; i < data.blocks.Count; i++)
            {
               foreach (string id in data.blocks[i].textureIDs)
                {
                    if (!blockUses.ContainsKey(id))
                        continue;
                    if (!blockUses.ContainsKey(i.ToString()))
                        blockUses.Add(i.ToString(), 0);
                    blockUses[i.ToString()] += blockUses[id];
                    blockUses.Remove(id);
                }
            }
        }

        private void GenerateNormal()
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    Texture closest = GetClosestTexture(bitmap.GetPixel(i, j));
                    field.Add(closest);
                }
            }
        }

        private void GenerateDithered()
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    Color originalColor = bitmap.GetPixel(i, j);
                    Texture closestTexture = GetClosestTexture(originalColor);

                    int errorR = originalColor.R - closestTexture.avgColor.R;
                    int errorG = originalColor.G - closestTexture.avgColor.G;
                    int errorB = originalColor.B - closestTexture.avgColor.B;

                    if (i < width - 1)
                        bitmap.SetPixel(i + 1, j, MakeDitheredColor(bitmap.GetPixel(i + 1, j), errorR, errorG, errorB, 7.0 / 16.0));
                    if (j < height - 1 && i > 0)
                        bitmap.SetPixel(i - 1, j + 1, MakeDitheredColor(bitmap.GetPixel(i - 1, j + 1), errorR, errorG, errorB, 3.0 / 16.0));
                    if (j < height - 1)
                        bitmap.SetPixel(i, j + 1, MakeDitheredColor(bitmap.GetPixel(i, j + 1), errorR, errorG, errorB, 5.0 / 16.0));
                    if (i < width - 1 && j < height - 1)
                        bitmap.SetPixel(i + 1, j + 1, MakeDitheredColor(bitmap.GetPixel(i + 1, j + 1), errorR, errorG, errorB, 1.0 / 16.0));

                    //closestTexture.uses++;
                    field.Add(closestTexture);
                }
            }
        }

        private Color MakeDitheredColor(Color oldColor, int errorR, int errorG, int errorB, double errorCoeff)
        {
            //errorCoeff *= 0.5;
            int newR = oldColor.R + (int)(errorR * errorCoeff);
            int newG = oldColor.G + (int)(errorG * errorCoeff);
            int newB = oldColor.B + (int)(errorB * errorCoeff);

            if (newR < 0)
                newR = 0;
            if (newG < 0)
                newG = 0;
            if (newB < 0)
                newB = 0;
            if (newR > 255)
                newR = 255;
            if (newG > 255)
                newG = 255;
            if (newB > 255)
                newB = 255;

            return Color.FromArgb(newR, newG, newB);
        }

        public Bitmap GetImage()
        {
            Bitmap result = new Bitmap(width * 16, height * 16);
            Graphics gr = Graphics.FromImage(result);

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    gr.DrawImage(field[i + j * width].texture, new Point(i * 16, j * 16));
                }
            }

            return result;
        }

        private Texture GetClosestTexture(Color color)
        {
            double minDist = Double.PositiveInfinity;
            Texture closestTexture = null;
            foreach (KeyValuePair<string,Texture> pair in data.textures)
            {
                if (!pair.Value.used)
                    continue;
                switch (orientation)
                {
                    case BlockOrientation.BOTTOM:
                        if (!pair.Value.bottom)
                            continue;
                        break;
                    case BlockOrientation.TOP:
                        if (!pair.Value.top)
                            continue;
                        break;
                    case BlockOrientation.VERTICAL:
                        if (!(pair.Value.north && pair.Value.east && pair.Value.south && pair.Value.west))
                            continue;
                        break;
                }
                double dist = FindDistance(color, pair.Value.avgColor);
                if (dist < minDist)
                {
                    closestTexture = pair.Value;
                    minDist = dist;
                }
            }
            return closestTexture;
        }

        private double FindDistance(Color c1, Color c2)
        {
            return 2 * Math.Pow(c1.R - c2.R, 2) + 4 * Math.Pow(c1.G - c2.G, 2) + 3 * Math.Pow(c1.B - c2.B, 2);
        }
    }
}
