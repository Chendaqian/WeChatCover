using System.Drawing;

namespace WeChatCover
{
    internal static class Utilities
    {
        public static Bitmap Mosaic(Bitmap bmpOrigin, int radius)
        {
            Bitmap bmpResult = new Bitmap(bmpOrigin.Width, bmpOrigin.Height);

            for (int y = radius; y < bmpOrigin.Height; y += radius * 2 + 1)
            {
                for (int x = radius; x < bmpOrigin.Width; x += radius * 2 + 1)
                {
                    int sumA = 0;
                    int sumR = 0;
                    int sumG = 0;
                    int sumB = 0;
                    int pixelCount = 0;

                    for (int y1 = y - radius; y1 < y + radius + 1; ++y1)
                    {
                        if (y1 >= bmpOrigin.Height)
                            break;

                        for (int x1 = x - radius; x1 < x + radius + 1; ++x1)
                        {
                            if (x1 >= bmpOrigin.Width)
                                break;

                            Color pixel = bmpOrigin.GetPixel(x1, y1);
                            sumA += pixel.A;
                            sumR += pixel.R;
                            sumG += pixel.G;
                            sumB += pixel.B;
                            ++pixelCount;
                        }
                    }

                    int avgA = sumA / pixelCount;
                    int avgR = sumR / pixelCount;
                    int avgG = sumG / pixelCount;
                    int avgB = sumB / pixelCount;

                    Color newColor = Color.FromArgb(avgA, avgR, avgG, avgB);

                    for (int y1 = y - radius; y1 < y + radius + 1; ++y1)
                    {
                        if (y1 >= bmpOrigin.Height)
                            break;

                        for (int x1 = x - radius; x1 < x + radius + 1; ++x1)
                        {
                            if (x1 >= bmpOrigin.Width)
                                break;

                            bmpResult.SetPixel(x1, y1, newColor);
                        }
                    }
                }
            }

            return bmpResult;
        }
    }
}
