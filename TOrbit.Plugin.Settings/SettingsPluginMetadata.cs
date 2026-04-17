using TOrbit.Plugin.Core.Base;

namespace TOrbit.Plugin.Settings;

public sealed class SettingsPluginMetadata : PluginBaseMetadata
{
    public static SettingsPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.settings";
    public override string Name => "设置";
    public override string Version => "1.0.1";
    public override string Description => "管理主题、配色方案和工作区相关设置。";
    public override string Author => "T-Orbit";
    public override string Icon => "Cog";
    public override string Tags => "System,Theme,Builtin";
}
