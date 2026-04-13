using Tranbok.Tools.Plugin.Core.Enums;

namespace Tranbok.Tools.Plugin.Core;

public sealed record PluginLoadRequest(
    PluginManifest Manifest,
    PluginLoadMode? LoadMode = null,
    bool AutoStart = true);
