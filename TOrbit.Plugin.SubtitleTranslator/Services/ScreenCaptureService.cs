using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SkiaSharp;
using TOrbit.Plugin.SubtitleTranslator.Models;

namespace TOrbit.Plugin.SubtitleTranslator.Services;

public sealed class ScreenCaptureService
{
    [SupportedOSPlatform("windows")]
    public SubtitleRegion CreatePrototypeRegion()
    {
        EnsureWindows();

        var screenWidth = GetSystemMetrics(SystemMetric.CxScreen);
        var screenHeight = GetSystemMetrics(SystemMetric.CyScreen);

        var width = Math.Clamp((int)(screenWidth * 0.66), 720, screenWidth);
        var height = Math.Clamp((int)(screenHeight * 0.14), 120, 240);
        var x = Math.Max(0, (screenWidth - width) / 2);
        var y = Math.Max(0, screenHeight - height - 120);

        return new SubtitleRegion(x, y, width, height);
    }

    [SupportedOSPlatform("windows")]
    public Task<SKBitmap> CaptureAsync(SubtitleRegion region, CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();

        if (!region.IsValid)
            throw new ArgumentException("The capture region must have a positive width and height.", nameof(region));

        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(region.X, region.Y, 0, 0, new Size(region.Width, region.Height), CopyPixelOperation.SourceCopy);
        }

        return Task.FromResult(ToSkBitmap(bitmap));
    }

    [SupportedOSPlatform("windows")]
    private static SKBitmap ToSkBitmap(Bitmap bitmap)
    {
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var skBitmap = new SKBitmap(info);

            unsafe
            {
                Buffer.MemoryCopy(
                    source: (void*)data.Scan0,
                    destination: (void*)skBitmap.GetPixels(),
                    destinationSizeInBytes: skBitmap.RowBytes * skBitmap.Height,
                    sourceBytesToCopy: data.Stride * data.Height);
            }

            return skBitmap;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The prototype screen capture service currently supports Windows only.");
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(SystemMetric smIndex);

    private enum SystemMetric
    {
        CxScreen = 0,
        CyScreen = 1
    }
}
