using TOrbit.Plugin.SubtitleTranslator.Services;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class SubtitleStabilizerServiceTests
{
    [Fact]
    public void Process_SuppressesNearDuplicateWithinRefreshWindow()
    {
        var service = new SubtitleStabilizerService();
        var timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var options = new SubtitleStabilizerOptions(
            SimilarityThreshold: 0.9,
            MinRefreshInterval: TimeSpan.FromMilliseconds(800),
            StabilizationWindow: TimeSpan.FromMilliseconds(1200));

        var first = service.Process("hello world", timestamp, options);
        var duplicate = service.Process("hello world!", timestamp.AddMilliseconds(200), options);

        Assert.True(first.IsNewSubtitle);
        Assert.True(first.ShouldTranslate);
        Assert.False(duplicate.IsNewSubtitle);
        Assert.False(duplicate.ShouldTranslate);
        Assert.Equal("hello world", duplicate.StableText);
    }

    [Fact]
    public void Process_AcceptsIncrementalGrowthWithinWindow()
    {
        var service = new SubtitleStabilizerService();
        var timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var options = new SubtitleStabilizerOptions(
            SimilarityThreshold: 0.9,
            MinRefreshInterval: TimeSpan.FromMilliseconds(800),
            StabilizationWindow: TimeSpan.FromMilliseconds(1200));

        service.Process("hello", timestamp, options);
        var growth = service.Process("hello world", timestamp.AddMilliseconds(400), options);

        Assert.True(growth.IsNewSubtitle);
        Assert.True(growth.ShouldTranslate);
        Assert.Equal("hello world", growth.StableText);
    }

    [Fact]
    public void Reset_ClearsAcceptedState()
    {
        var service = new SubtitleStabilizerService();
        var timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        service.Process("first subtitle", timestamp);
        service.Reset();
        var afterReset = service.Process("first subtitle", timestamp.AddSeconds(1));

        Assert.True(afterReset.IsNewSubtitle);
        Assert.True(afterReset.ShouldTranslate);
    }
}
