using TOrbit.Core.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Enums;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class PluginDependencyResolverTests
{
    [Fact]
    public async Task ResolveAsync_OrdersDependenciesBeforeDependents()
    {
        var resolver = new PluginDependencyResolver();
        var shared = CreateManifest("torbit.shared");
        var feature = CreateManifest(
            "torbit.feature",
            [new PluginDependency("torbit.shared", "1.0.0", false)]);

        var graph = await resolver.ResolveAsync([feature, shared]);

        Assert.Equal(["torbit.shared", "torbit.feature"], graph.LoadOrder);
    }

    [Fact]
    public async Task ResolveAsync_ThrowsForMissingRequiredDependency()
    {
        var resolver = new PluginDependencyResolver();
        var feature = CreateManifest(
            "torbit.feature",
            [new PluginDependency("torbit.missing", "*", false)]);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync([feature]));

        Assert.Contains("torbit.missing", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolveAsync_ThrowsForCircularDependency()
    {
        var resolver = new PluginDependencyResolver();
        var pluginA = CreateManifest(
            "torbit.a",
            [new PluginDependency("torbit.b", "*", false)]);
        var pluginB = CreateManifest(
            "torbit.b",
            [new PluginDependency("torbit.a", "*", false)]);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync([pluginA, pluginB]));

        Assert.Contains("Circular plugin dependency", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolveAsync_IgnoresMissingOptionalDependency()
    {
        var resolver = new PluginDependencyResolver();
        var feature = CreateManifest(
            "torbit.feature",
            [new PluginDependency("torbit.optional", "*", true)]);

        var graph = await resolver.ResolveAsync([feature]);

        Assert.Equal(["torbit.feature"], graph.LoadOrder);
    }

    private static PluginManifest CreateManifest(string id, IReadOnlyCollection<PluginDependency>? dependencies = null)
    {
        var descriptor = new PluginDescriptor(
            id,
            id,
            "1.0.0",
            $"{id}.dll",
            $"{id}.Entry",
            LoadMode: PluginLoadMode.Lazy,
            IsolationMode: PluginIsolationMode.AssemblyLoadContext,
            Kind: PluginKind.Visual);

        return new PluginManifest(descriptor, AppContext.BaseDirectory, dependencies ?? [], new Dictionary<string, string?>());
    }
}
