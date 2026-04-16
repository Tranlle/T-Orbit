using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Plugin.Core;

public sealed class PluginHandle
{
    public PluginHandle(string pluginId, IPlugin instance, PluginManifest manifest, PluginContext context)
    {
        PluginId = pluginId;
        Instance = instance;
        Manifest = manifest;
        Context = context;
    }

    public string PluginId { get; }

    public IPlugin Instance { get; }

    public PluginManifest Manifest { get; }

    public PluginContext Context { get; }

    public PluginState State { get; private set; } = PluginState.Loaded;

    public void SetState(PluginState state)
    {
        State = state;
    }
}
