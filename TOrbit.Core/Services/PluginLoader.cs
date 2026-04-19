using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using TOrbit.Core.Constants;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Tools;

namespace TOrbit.Core.Services;

public sealed class PluginLoader : IPluginLoader
{
    private readonly IPluginToolRegistry _toolRegistry;
    private readonly HostEnvironmentInfo _hostEnvironment;
    private readonly IServiceProvider _serviceProvider;

    public PluginLoader(IPluginToolRegistry toolRegistry, IServiceProvider serviceProvider)
    {
        _toolRegistry = toolRegistry;
        _serviceProvider = serviceProvider;
        _hostEnvironment = new HostEnvironmentInfo(
            ToolHostConstants.HostName,
            ToolHostConstants.HostVersion,
            Environment.Version.ToString(),
            "net10.0",
            OperatingSystem.IsWindows() ? "Windows" : Environment.OSVersion.Platform.ToString(),
            ToolHostConstants.PluginApiVersion);
    }

    public async Task<PluginLoadResult> LoadAsync(PluginLoadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var manifest = request.Manifest;
        var assemblyPath = ResolveAssemblyPath(manifest);
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        var pluginType = assembly.GetType(manifest.Descriptor.EntryType, throwOnError: true)
            ?? throw new InvalidOperationException(
                $"Plugin entry type '{manifest.Descriptor.EntryType}' not found in '{assemblyPath}'.");

        if (ActivatorUtilities.CreateInstance(_serviceProvider, pluginType) is not IPlugin plugin)
        {
            throw new InvalidOperationException(
                $"Plugin entry type '{manifest.Descriptor.EntryType}' does not implement IPlugin.");
        }

        var warnings = new List<string>();
        var compatibility = EvaluateCompatibility(manifest, plugin.Descriptor, warnings);
        if (compatibility.Status == PluginCompatibilityStatus.Incompatible)
            throw new InvalidOperationException(compatibility.Message);

        var context = new PluginContext(
            manifest.Id,
            manifest.BaseDirectory,
            _toolRegistry,
            _hostEnvironment,
            manifest.IsolationMode,
            new Dictionary<string, object?>
            {
                ["manifest"] = manifest,
                ["capabilities"] = manifest.Descriptor.Capabilities ?? []
            });

        await plugin.InitializeAsync(context, cancellationToken);
        if (request.AutoStart)
            await plugin.StartAsync(cancellationToken);

        var handle = new PluginHandle(manifest.Id, plugin, manifest, context)
        ;
        handle.SetState(request.AutoStart ? PluginState.Running : PluginState.Loaded);

        return new PluginLoadResult(handle, compatibility, warnings);
    }

    public async Task UnloadAsync(PluginHandle pluginHandle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pluginHandle);

        await pluginHandle.Instance.StopAsync(cancellationToken);
        await pluginHandle.Instance.DisposeAsync();
        pluginHandle.SetState(PluginState.Unloaded);
    }

    private static string ResolveAssemblyPath(PluginManifest manifest)
    {
        var assemblyPath = manifest.Descriptor.EntryAssembly;
        if (Path.IsPathRooted(assemblyPath))
            return assemblyPath;

        return Path.GetFullPath(Path.Combine(manifest.BaseDirectory, assemblyPath));
    }

    internal static PluginCompatibilityResult EvaluateCompatibility(
        PluginManifest manifest,
        PluginDescriptor runtimeDescriptor,
        ICollection<string> warnings)
    {
        if (!string.Equals(manifest.Id, runtimeDescriptor.Id, StringComparison.OrdinalIgnoreCase))
        {
            return new PluginCompatibilityResult(
                PluginCompatibilityStatus.Incompatible,
                $"Manifest plugin id '{manifest.Id}' does not match runtime descriptor id '{runtimeDescriptor.Id}'.");
        }

        if (!string.Equals(manifest.Descriptor.EntryType, runtimeDescriptor.EntryType, StringComparison.Ordinal))
        {
            return new PluginCompatibilityResult(
                PluginCompatibilityStatus.Incompatible,
                $"Manifest entry type '{manifest.Descriptor.EntryType}' does not match runtime descriptor entry type '{runtimeDescriptor.EntryType}'.");
        }

        if (!string.Equals(manifest.Descriptor.EntryAssembly, runtimeDescriptor.EntryAssembly, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                $"Manifest entry assembly '{manifest.Descriptor.EntryAssembly}' does not match runtime descriptor entry assembly '{runtimeDescriptor.EntryAssembly}'.");
        }

        if (!string.Equals(manifest.Descriptor.Name, runtimeDescriptor.Name, StringComparison.Ordinal))
        {
            warnings.Add(
                $"Manifest name '{manifest.Descriptor.Name}' does not match runtime descriptor name '{runtimeDescriptor.Name}'.");
        }

        if (manifest.Descriptor.Kind != runtimeDescriptor.Kind)
        {
            warnings.Add(
                $"Manifest kind '{manifest.Descriptor.Kind}' does not match runtime descriptor kind '{runtimeDescriptor.Kind}'.");
        }

        if (!string.Equals(manifest.Version, runtimeDescriptor.Version, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                $"Manifest version '{manifest.Version}' does not match runtime descriptor version '{runtimeDescriptor.Version}'.");
        }

        return warnings.Count == 0
            ? new PluginCompatibilityResult(PluginCompatibilityStatus.Compatible)
            : new PluginCompatibilityResult(
                PluginCompatibilityStatus.Warning,
                "Plugin manifest and runtime descriptor differ.",
                warnings.ToArray());
    }
}
