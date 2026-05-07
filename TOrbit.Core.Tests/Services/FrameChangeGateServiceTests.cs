using SkiaSharp;
using TOrbit.Plugin.SubtitleTranslator.Services;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class FrameChangeGateServiceTests
{
    [Fact]
    public void Evaluate_FirstFrameDoesNotSkip()
    {
        var service = new FrameChangeGateService();
        using var bitmap = CreateBitmap(64, 32, SKColors.Black);

        var result = service.Evaluate(bitmap, enabled: true, threshold: 0.03);

        Assert.False(result.ShouldSkipOcr);
        Assert.True(result.Delta < 0);
    }

    [Fact]
    public void Evaluate_IdenticalFrameSkipsWhenDetectionEnabled()
    {
        var service = new FrameChangeGateService();
        using var first = CreateBitmap(64, 32, SKColors.Black);
        using var second = CreateBitmap(64, 32, SKColors.Black);

        service.Evaluate(first, enabled: true, threshold: 0.03);
        var result = service.Evaluate(second, enabled: true, threshold: 0.03);

        Assert.True(result.ShouldSkipOcr);
        Assert.InRange(result.Delta, 0, 0.001);
    }

    [Fact]
    public void Evaluate_ChangedFrameDoesNotSkipWhenDifferenceExceedsThreshold()
    {
        var service = new FrameChangeGateService();
        using var first = CreateBitmap(64, 32, SKColors.Black);
        using var second = CreateBitmap(64, 32, SKColors.White);

        service.Evaluate(first, enabled: true, threshold: 0.03);
        var result = service.Evaluate(second, enabled: true, threshold: 0.03);

        Assert.False(result.ShouldSkipOcr);
        Assert.True(result.Delta > 0.5);
    }

    [Fact]
    public void Evaluate_DisabledDetectionNeverSkips()
    {
        var service = new FrameChangeGateService();
        using var first = CreateBitmap(64, 32, SKColors.Black);
        using var second = CreateBitmap(64, 32, SKColors.Black);

        service.Evaluate(first, enabled: false, threshold: 0.03);
        var result = service.Evaluate(second, enabled: false, threshold: 0.03);

        Assert.False(result.ShouldSkipOcr);
    }

    private static SKBitmap CreateBitmap(int width, int height, SKColor color)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        canvas.Flush();
        return bitmap;
    }
}
