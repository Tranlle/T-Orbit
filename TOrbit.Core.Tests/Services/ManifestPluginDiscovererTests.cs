using TOrbit.Core.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Enums;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class ManifestPluginDiscovererTests
{
    [Fact]
    public async Task DiscoverAsync_ParsesManifestCapabilitiesAndMetadata()
    {
        var root = CreateTempDirectory();

        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "plugin.json"), """
            {
              "id": "torbit.test.promptor",
              "name": "Test Promptor",
              "version": "1.2.3",
              "entryAssembly": "Test.Plugin.dll",
              "entryType": "Test.Plugin.Entry",
              "description": "manifest test",
              "kind": "Visual",
              "capabilities": ["Network", "Secrets"],
              "dependencies": [
                { "pluginId": "torbit.shared", "versionRange": "1.0.0", "isOptional": false }
              ],
              "metadata": {
                "displayCategory": "AI"
              }
            }
            """);

            var discoverer = new ManifestPluginDiscoverer();
            var manifests = await discoverer.DiscoverAsync(new PluginDiscoveryOptions(root, Recursive: true));
            var manifest = Assert.Single(manifests);

            Assert.Equal("torbit.test.promptor", manifest.Id);
            Assert.Equal("Test.Plugin.dll", manifest.Descriptor.EntryAssembly);
            Assert.Equal([PluginCapability.Network, PluginCapability.Secrets], manifest.Descriptor.Capabilities);
            Assert.Equal("AI", manifest.Metadata["displayCategory"]);

            var dependency = Assert.Single(manifest.Dependencies);
            Assert.Equal("torbit.shared", dependency.PluginId);
            Assert.Equal("1.0.0", dependency.VersionRange);
            Assert.False(dependency.IsOptional);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DiscoverAsync_ThrowsForDuplicatePluginIds()
    {
        var root = CreateTempDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "a"));
            Directory.CreateDirectory(Path.Combine(root, "b"));

            await File.WriteAllTextAsync(Path.Combine(root, "a", "plugin.json"), CreateManifestJson("torbit.duplicate", "A.Plugin.dll"));
            await File.WriteAllTextAsync(Path.Combine(root, "b", "plugin.json"), CreateManifestJson("torbit.duplicate", "B.Plugin.dll"));

            var discoverer = new ManifestPluginDiscoverer();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => discoverer.DiscoverAsync(new PluginDiscoveryOptions(root, Recursive: true)));

            Assert.Contains("Duplicate plugin id 'torbit.duplicate'", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DiscoverAsync_ThrowsForInvalidManifestRules()
    {
        var root = CreateTempDirectory();

        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "plugin.json"), """
            {
              "id": "invalid",
              "name": "Invalid Plugin",
              "version": "1.0.0",
              "entryAssembly": "plugin.exe",
              "entryType": "Invalid.Plugin",
              "dependencies": [
                { "pluginId": "invalid", "versionRange": "*", "isOptional": false }
              ]
            }
            """);

            var discoverer = new ManifestPluginDiscoverer();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => discoverer.DiscoverAsync(new PluginDiscoveryOptions(root, Recursive: true)));

            Assert.Contains("invalid plugin id", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string CreateManifestJson(string id, string assemblyName) => $$"""
    {
      "id": "{{id}}",
      "name": "Test Plugin",
      "version": "1.0.0",
      "entryAssembly": "{{assemblyName}}",
      "entryType": "Test.Plugin.Entry"
    }
    """;

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "TOrbit.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
