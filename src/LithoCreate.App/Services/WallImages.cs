using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LithoCreate.Core;

namespace LithoCreate.App.Services;

internal static class WallImages
{
    public static GrayImage?[] Decode(byte[]?[] bytes, CancellationToken token) =>
        bytes.Select(image => image is null ? null : ImageProcessor.Process(ImageProcessor.Decode(image, 1400), new(), token).Gray).ToArray();

    public static GrayImage Atlas(GrayImage main, GrayImage?[] walls, CancellationToken token)
    {
        const int tile = 256, width = tile * 4;
        var pixels = new byte[width * tile];
        for (int j = 0; j < tile; j++)
        {
            token.ThrowIfCancellationRequested();
            for (int k = 0; k < walls.Length; k++)
            {
                GrayImage source = walls[k] ?? main;
                for (int i = 0; i < tile; i++)
                    pixels[j * width + k * tile + i] = (byte)Math.Round(source.Sample(i / 255d, j / 255d) * 255);
            }
        }
        return new(width, tile, pixels);
    }

    public static byte[] Text(string value, double ratio)
    {
        const int width = 800;
        int height = Math.Clamp((int)Math.Round(width / ratio), 250, 1400);
        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        double size = 110;
        var text = new FormattedText(value, CultureInfo.GetCultureInfo("de-DE"), FlowDirection.LeftToRight,
            typeface, size, Brushes.Black, 1) { MaxTextWidth = width * .80, TextAlignment = TextAlignment.Center };
        while (text.Height > height * .73 && size > 8)
        {
            size *= .86;
            text.SetFontSize(size);
        }
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            drawing.DrawText(text, new Point(width * .10, (height - text.Height) / 2));
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return ImageProcessor.Png(bitmap);
    }
}
