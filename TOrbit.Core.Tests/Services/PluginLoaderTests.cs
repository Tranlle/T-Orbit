using TOrbit.Core.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Enums;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class PluginLoaderTests
{
    [Fact]
    public void EvaluateCompatibility_ReturnsWarningForDescriptorDrift()
    {
        var warnings = new List<string>();
        var manifest = CreateManifest(
            new PluginDescriptor(
                "torbit.promptor",
                "Promptor",
                "1.0.0",
                "Promptor.dll",
                "Promptor.Entry",
                Kind: PluginKind.Visual));
        var runtime = new PluginDescriptor(
            "torbit.promptor",
            "Promptor Runtime",
            "1.1.0",
            "Promptor.Runtime.dll",
            "Promptor.Entry",
            Kind: PluginKind.Service);

        var result = PluginLoader.EvaluateCompatibility(manifest, runtime, warnings);

        Assert.Equal(PluginCompatibilityStatus.Warning, result.Status);
        Assert.NotNull(result.Details);
        Assert.Contains(result.Details!, detail => detail.Contains("version", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Details!, detail => detail.Contains("entry assembly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Details!, detail => detail.Contains("name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Details!, detail => detail.Contains("kind", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EvaluateCompatibility_ReturnsIncompatibleForEntryTypeMismatch()
    {
        var warnings = new List<string>();
        var manifest = CreateManifest(
            new PluginDescriptor(
                "torbit.promptor",
                "Promptor",
                "1.0.0",
                "Promptor.dll",
                "Promptor.Entry"));
        var runtime = new PluginDescriptor(
            "torbit.promptor",
            "Promptor",
            "1.0.0",
            "Promptor.dll",
            "Promptor.OtherEntry");

        var result = PluginLoader.EvaluateCompatibility(manifest, runtime, warnings);

        Assert.Equal(PluginCompatibilityStatus.Incompatible, result.Status);
        Assert.Contains("entry type", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static PluginManifest CreateManifest(PluginDescriptor descriptor)
        => new(
            descriptor,
            AppContext.BaseDirectory,
            Array.Empty<PluginDependency>(),
            new Dictionary<string, string?>());
}
