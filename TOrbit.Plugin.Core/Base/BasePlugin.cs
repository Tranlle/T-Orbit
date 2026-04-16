using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Core.Base;

public abstract class BasePlugin : IPlugin, IPluginViewProvider
{
    private PluginContext? _context;

    public PluginContext Context => _context ?? throw new InvalidOperationException("Plugin has not been initialized.");
    public bool IsInitialized => _context is not null;

    public abstract PluginDescriptor Descriptor { get; }

    public PluginState State { get; private set; } = PluginState.Discovered;

    public void SetState(PluginState state) => State = state;

    protected static PluginDescriptor CreateDescriptor<TPlugin>(
        PluginBaseMetadata metadata,
        PluginLoadMode loadMode = PluginLoadMode.Lazy,
        PluginIsolationMode isolationMode = PluginIsolationMode.AssemblyLoadContext)
        where TPlugin : IPlugin
    {
        metadata.ValidateId();

        var pluginType = typeof(TPlugin);
        return new PluginDescriptor(
            metadata.Id,
            metadata.Name,
            metadata.Version,
            pluginType.Assembly.GetName().Name ?? pluginType.Assembly.FullName ?? pluginType.Name,
            pluginType.FullName ?? pluginType.Name,
            metadata.Description,
            metadata.Author,
            metadata.Icon,
            metadata.Tags,
            loadMode,
            isolationMode,
            metadata.VariableDefinitions.Count > 0 ? metadata.VariableDefinitions : null,
            metadata.Kind,
            metadata.Capabilities.Count > 0 ? metadata.Capabilities : null);
    }

    public virtual async ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        State = PluginState.Loaded;
        await OnInitializeAsync(context, cancellationToken);
    }

    public virtual async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Running;
        await OnStartAsync(cancellationToken);
    }

    public virtual async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Stopping;
        await OnStopAsync(cancellationToken);
        State = PluginState.Loaded;
    }

    public virtual async ValueTask DisposeAsync()
    {
        await OnDisposeAsync();
        _context = null;
        State = PluginState.Unloaded;
    }

    protected virtual ValueTask OnInitializeAsync(PluginContext context, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    protected virtual ValueTask OnStartAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    protected virtual ValueTask OnStopAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public virtual object GetMainView() => CreateDefaultView();

    protected virtual object CreateDefaultView() => new PluginDefaultViewModel(
        Descriptor.Name,
        string.IsNullOrWhiteSpace(Descriptor.Description)
            ? "This plugin does not provide a custom page."
            : Descriptor.Description);

    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;
}
