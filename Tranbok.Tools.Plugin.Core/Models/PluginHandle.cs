using Tranbok.Tools.Plugin.Core.Abstractions;
using Tranbok.Tools.Plugin.Core.Enums;

namespace Tranbok.Tools.Plugin.Core;

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

    public PluginState State { get; internal set; } = PluginState.Loaded;
}
