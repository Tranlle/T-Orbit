using SkiaSharp;

namespace TOrbit.Plugin.SubtitleTranslator.Services;

public sealed class ImagePreprocessService
{
    public SKBitmap Preprocess(SKBitmap source, ImagePreprocessOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        options ??= ImagePreprocessOptions.Default;

        using var padded = ApplyPadding(source, options.Padding);
        using var scaled = ApplyScale(padded, options.ScaleFactor);
        using var grayscale = ApplyGrayscale(scaled);
        using var contrasted = ApplyContrast(grayscale, options.Contrast);

        if (!options.EnableThreshold)
            return contrasted.Copy();

        return ApplyThreshold(contrasted, options.Threshold);
    }

    private static SKBitmap ApplyPadding(SKBitmap source, int padding)
    {
        if (padding <= 0)
            return source.Copy();

        var target = new SKBitmap(source.Width + (padding * 2), source.Height + (padding * 2), source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Black);
        canvas.DrawBitmap(source, padding, padding);
        canvas.Flush();
        return target;
    }

    private static SKBitmap ApplyScale(SKBitmap source, float scaleFactor)
    {
        if (scaleFactor <= 1f)
            return source.Copy();

        var width = Math.Max(1, (int)MathF.Round(source.Width * scaleFactor));
        var height = Math.Max(1, (int)MathF.Round(source.Height * scaleFactor));
        var resized = source.Resize(new SKImageInfo(width, height, source.ColorType, source.AlphaType), SKSamplingOptions.Default);
        return resized ?? source.Copy();
    }

    private static SKBitmap ApplyGrayscale(SKBitmap source)
    {
        var target = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(target);
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(
            [
                0.2126f, 0.7152f, 0.0722f, 0, 0,
                0.2126f, 0.7152f, 0.0722f, 0, 0,
                0.2126f, 0.7152f, 0.0722f, 0, 0,
                0, 0, 0, 1, 0
            ])
        };
        canvas.DrawBitmap(source, 0, 0, paint);
        canvas.Flush();
        return target;
    }

    private static SKBitmap ApplyContrast(SKBitmap source, float contrast)
    {
        if (Math.Abs(contrast - 1f) < 0.01f)
            return source.Copy();

        var target = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(target);
        var translate = 128f * (1f - contrast);
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(
            [
                contrast, 0, 0, 0, translate,
                0, contrast, 0, 0, translate,
                0, 0, contrast, 0, translate,
                0, 0, 0, 1, 0
            ])
        };
        canvas.DrawBitmap(source, 0, 0, paint);
        canvas.Flush();
        return target;
    }

    private static SKBitmap ApplyThreshold(SKBitmap source, byte threshold)
    {
        var target = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var color = source.GetPixel(x, y);
                var value = color.Red >= threshold ? (byte)255 : (byte)0;
                target.SetPixel(x, y, new SKColor(value, value, value, color.Alpha));
            }
        }

        return target;
    }
}

public sealed record ImagePreprocessOptions(
    int Padding,
    float ScaleFactor,
    float Contrast,
    bool EnableThreshold,
    byte Threshold)
{
    public static ImagePreprocessOptions Default { get; } = new(
        Padding: 20,
        ScaleFactor: 1.8f,
        Contrast: 1.35f,
        EnableThreshold: true,
        Threshold: 148);
}
