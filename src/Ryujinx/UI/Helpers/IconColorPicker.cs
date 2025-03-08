using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ryujinx.Ava.UI.Helpers
{
    public static class IconColorPicker
    {
        private const int ColorsPerLine = 64;
        private const int TotalColors = ColorsPerLine * ColorsPerLine;

        private const int UvQuantBits = 3;
        private const int UvQuantShift = BitsPerComponent - UvQuantBits;

        private const int SatQuantBits = 5;
        private const int SatQuantShift = BitsPerComponent - SatQuantBits;

        private const int BitsPerComponent = 8;

        private const int CutOffLuminosity = 64;

        private readonly struct PaletteColor(int qck, byte r, byte g, byte b)
        {
            public int Qck => qck;
            public byte R => r;
            public byte G => g;
            public byte B => b;
        }

        public static SKColor GetFilteredColor(SKBitmap image)
        {
            SKColor color = GetColor(image);


            // We don't want colors that are too dark.
            // If the color is too dark, make it brighter by reducing the range
            // and adding a constant color.
            int luminosity = GetColorApproximateLuminosity(color.Red, color.Green, color.Blue);
            if (luminosity < CutOffLuminosity)
            {
                color = new SKColor(
                    (byte)Math.Min(CutOffLuminosity + color.Red, byte.MaxValue),
                    (byte)Math.Min(CutOffLuminosity + color.Green, byte.MaxValue),
                    (byte)Math.Min(CutOffLuminosity + color.Blue, byte.MaxValue));
            }

            return color;
        }

        public static SKColor GetColor(SKBitmap image)
        {
            PaletteColor[] colors = new PaletteColor[TotalColors];
            Dictionary<int, int> dominantColorBin = new();

            SKColor[] buffer = GetBuffer(image);

            int i = 0;
            int maxHitCount = 0;

            for (int y = 0; y < image.Height; y++)
            {
                int yOffset = y * image.Width;

                for (int x = 0; x < image.Width && i < TotalColors; x++)
                {
                    int offset = x + yOffset;

                    SKColor pixel = buffer[offset];
                    byte cr = pixel.Red;
                    byte cg = pixel.Green;
                    byte cb = pixel.Blue;

                    int qck = GetQuantizedColorKey(cr, cg, cb);

                    if (dominantColorBin.TryGetValue(qck, out int hitCount))
                    {
                        dominantColorBin[qck] = hitCount + 1;

                        if (maxHitCount < hitCount)
                        {
                            maxHitCount = hitCount;
                        }
                    }
                    else
                    {
                        dominantColorBin.Add(qck, 1);
                    }

                    colors[i++] = new PaletteColor(qck, cr, cg, cb);
                }
            }

            int highScore = -1;
            PaletteColor bestCandidate = default;

            for (i = 0; i < TotalColors; i++)
            {
                int score = GetColorScore(dominantColorBin, maxHitCount, colors[i]);

                if (highScore < score)
                {
                    highScore = score;
                    bestCandidate = colors[i];
                }
            }

            return new SKColor(bestCandidate.R, bestCandidate.G, bestCandidate.B);
        }

        public static SKColor[] GetBuffer(SKBitmap image)
        {
            SKColor[] pixels = new SKColor[image.Width * image.Height];

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    pixels[x + y * image.Width] = image.GetPixel(x, y);
                }
            }

            return pixels;
        }

        private static int GetColorScore(Dictionary<int, int> dominantColorBin, int maxHitCount, PaletteColor color)
        {
            int hitCount = dominantColorBin[color.Qck];
            int balancedHitCount = BalanceHitCount(hitCount, maxHitCount);
            int quantSat = (GetColorSaturation(color) >> SatQuantShift) << SatQuantShift;
            int value = GetColorValue(color);

            // If the color is rarely used on the image,
            // then chances are that there's a better candidate, even if the saturation value
            // is high. By multiplying the saturation value with a weight, we can lower
            // it if the color is almost never used (hit count is low).
            int satWeighted = quantSat;
            int satWeight = balancedHitCount << 5;
            if (satWeight < 0x100)
            {
                satWeighted = (satWeighted * satWeight) >> 8;
            }

            // Compute score from saturation and dominance of the color.
            // We prefer more vivid colors over dominant ones, so give more weight to the saturation.
            int score = ((satWeighted << 1) + balancedHitCount) * value;

            return score;
        }

        private static int GetColorSaturation(PaletteColor color)
        {
            int cMax = Math.Max(Math.Max(color.R, color.G), color.B);

            if (cMax == 0)
            {
                return 0;
            }

            int cMin = Math.Min(Math.Min(color.R, color.G), color.B);
            int delta = cMax - cMin;
            return (delta << 8) / cMax;
        }

        private static int GetColorValue(PaletteColor color) => Math.Max(Math.Max(color.R, color.G), color.B);

        private static int BalanceHitCount(int hitCount, int maxHitCount) => (hitCount << 8) / maxHitCount;

        private static int GetColorApproximateLuminosity(byte r, byte g, byte b) => (r + g + b) / 3;

        private static int GetQuantizedColorKey(byte r, byte g, byte b)
        {
            int u = ((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128;
            int v = ((112 * r - 94 * g - 18 * b + 128) >> 8) + 128;
            return (v >> UvQuantShift) | ((u >> UvQuantShift) << UvQuantBits);
        }
    }
}
