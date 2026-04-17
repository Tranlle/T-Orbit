using TOrbit.Plugin.Core.Base;

namespace TOrbit.Plugin.Monitor;

public sealed class MonitorPluginMetadata : PluginBaseMetadata
{
    public static MonitorPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.monitor";
    public override string Name => "插件监控";
    public override string Version => "1.0.0";
    public override string Description => "查看插件运行状态，执行启停/重启，并管理启用状态与排序。";
    public override string Author => "T-Orbit";
    public override string Icon => "MonitorHeart";
    public override string Tags => "System,Monitor,Builtin";
}
