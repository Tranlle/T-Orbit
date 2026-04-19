using TOrbit.Plugin.Core.Base;

namespace TOrbit.Plugin.Settings;

public sealed class SettingsPluginMetadata : PluginBaseMetadata
{
    public static SettingsPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.settings";
    public override string Name => "Settings";
    public override string Version => "1.0.1";
    public override string Description => "主题与变量";
    public override string Author => "T-Orbit";
    public override string Icon => "Cog";
    public override string Tags => "System,Theme,Builtin";
}
