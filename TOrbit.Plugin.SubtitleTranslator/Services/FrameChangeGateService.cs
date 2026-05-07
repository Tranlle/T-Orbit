using SkiaSharp;

namespace TOrbit.Plugin.SubtitleTranslator.Services;

public sealed class FrameChangeGateService
{
    private byte[]? _previousFrameSignature;

    public FrameChangeDecision Evaluate(SKBitmap bitmap, bool enabled, double threshold)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        if (!enabled)
            return new FrameChangeDecision(false, -1);

        var signature = CreateFrameSignature(bitmap);
        if (_previousFrameSignature is null)
        {
            _previousFrameSignature = signature;
            return new FrameChangeDecision(false, -1);
        }

        var delta = CalculateSignatureDelta(_previousFrameSignature, signature);
        _previousFrameSignature = signature;
        return new FrameChangeDecision(delta < Math.Clamp(threshold, 0, 1), delta);
    }

    public void Reset() => _previousFrameSignature = null;

    private static byte[] CreateFrameSignature(SKBitmap bitmap)
    {
        const int sampleColumns = 18;
        const int sampleRows = 10;

        var signature = new byte[sampleColumns * sampleRows];
        var index = 0;

        for (var row = 0; row < sampleRows; row++)
        {
            var y = Math.Min(bitmap.Height - 1, (int)Math.Round(row * (bitmap.Height - 1d) / Math.Max(1, sampleRows - 1)));
            for (var col = 0; col < sampleColumns; col++)
            {
                var x = Math.Min(bitmap.Width - 1, (int)Math.Round(col * (bitmap.Width - 1d) / Math.Max(1, sampleColumns - 1)));
                var color = bitmap.GetPixel(x, y);
                signature[index++] = (byte)Math.Clamp(
                    (int)Math.Round((0.2126 * color.Red) + (0.7152 * color.Green) + (0.0722 * color.Blue)),
                    0,
                    255);
            }
        }

        return signature;
    }

    private static double CalculateSignatureDelta(byte[] previous, byte[] current)
    {
        if (previous.Length == 0 || previous.Length != current.Length)
            return 1;

        double total = 0;
        for (var index = 0; index < previous.Length; index++)
            total += Math.Abs(previous[index] - current[index]) / 255d;

        return total / previous.Length;
    }
}

public readonly record struct FrameChangeDecision(bool ShouldSkipOcr, double Delta);
