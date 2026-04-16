using System.Text.Json;
using System.Text.Json.Serialization;
using TOrbit.Core.Constants;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Core.Services;

public sealed class ManifestPluginDiscoverer : IPluginDiscoverer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<IReadOnlyCollection<PluginManifest>> DiscoverAsync(
        PluginDiscoveryOptions options,
        CancellationToken cancellationToken = default)
    {
        var manifests = new List<PluginManifest>();
        var seenIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(options.RootDirectory))
            return Task.FromResult<IReadOnlyCollection<PluginManifest>>(manifests);

        var manifestPaths = Directory.EnumerateFiles(
            options.RootDirectory,
            ToolHostConstants.PluginManifestFileName,
            options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        foreach (var manifestPath in manifestPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var manifest = ParseManifest(manifestPath);

            if (seenIds.TryGetValue(manifest.Id, out var existingPath))
            {
                throw new InvalidOperationException(
                    $"Duplicate plugin id '{manifest.Id}' found in manifests '{existingPath}' and '{manifestPath}'.");
            }

            seenIds[manifest.Id] = manifestPath;
            manifests.Add(manifest);
        }

        return Task.FromResult<IReadOnlyCollection<PluginManifest>>(manifests);
    }

    private static PluginManifest ParseManifest(string manifestPath)
    {
        using var stream = File.OpenRead(manifestPath);
        var dto = JsonSerializer.Deserialize<PluginManifestDocument>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to parse plugin manifest: {manifestPath}");

        Validate(dto, manifestPath);

        var baseDirectory = Path.GetDirectoryName(manifestPath) ?? AppContext.BaseDirectory;
        var descriptor = new PluginDescriptor(
            dto.Id,
            dto.Name,
            dto.Version,
            dto.EntryAssembly,
            dto.EntryType,
            dto.Description,
            dto.Author,
            dto.Icon,
            dto.Tags,
            dto.LoadMode,
            dto.IsolationMode,
            null,
            dto.Kind,
            dto.Capabilities);

        return new PluginManifest(
            descriptor,
            baseDirectory,
            dto.Dependencies ?? [],
            dto.Metadata ?? new Dictionary<string, string?>());
    }

    private static void Validate(PluginManifestDocument dto, string manifestPath)
    {
        static void Require(string? value, string field, string manifestPath)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Manifest '{manifestPath}' is missing required field '{field}'.");
        }

        Require(dto.Id, nameof(dto.Id), manifestPath);
        Require(dto.Name, nameof(dto.Name), manifestPath);
        Require(dto.Version, nameof(dto.Version), manifestPath);
        Require(dto.EntryAssembly, nameof(dto.EntryAssembly), manifestPath);
        Require(dto.EntryType, nameof(dto.EntryType), manifestPath);

        if (!dto.Id.Contains('.', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Manifest '{manifestPath}' uses invalid plugin id '{dto.Id}'. Expected reverse-domain style such as 'torbit.example'.");
        }

        if (Path.IsPathRooted(dto.EntryAssembly))
        {
            throw new InvalidOperationException(
                $"Manifest '{manifestPath}' must use a relative entryAssembly path. Found '{dto.EntryAssembly}'.");
        }

        if (!dto.EntryAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Manifest '{manifestPath}' must point entryAssembly to a .dll file. Found '{dto.EntryAssembly}'.");
        }

        if (dto.Dependencies is not null)
        {
            var seenDependencyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependency in dto.Dependencies)
            {
                Require(dependency.PluginId, nameof(dependency.PluginId), manifestPath);

                if (string.Equals(dto.Id, dependency.PluginId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Manifest '{manifestPath}' declares a self dependency on '{dto.Id}'.");
                }

                if (!seenDependencyIds.Add(dependency.PluginId))
                {
                    throw new InvalidOperationException(
                        $"Manifest '{manifestPath}' declares duplicate dependency '{dependency.PluginId}'.");
                }
            }
        }
    }

    private sealed class PluginManifestDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string EntryAssembly { get; set; } = string.Empty;
        public string EntryType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? Icon { get; set; }
        public string? Tags { get; set; }
        public PluginLoadMode LoadMode { get; set; } = PluginLoadMode.Lazy;
        public PluginIsolationMode IsolationMode { get; set; } = PluginIsolationMode.AssemblyLoadContext;
        public PluginKind Kind { get; set; } = PluginKind.Visual;
        public List<PluginDependency>? Dependencies { get; set; }
        public List<PluginCapability>? Capabilities { get; set; }
        public Dictionary<string, string?>? Metadata { get; set; }
    }
}
