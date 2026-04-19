using TOrbit.Plugin.Core.Base;

namespace TOrbit.Plugin.Home;

public sealed class HomePluginMetadata : PluginBaseMetadata
{
    public static HomePluginMetadata Instance { get; } = new();

    public override string Id => "torbit.home";
    public override string Name => "Home";
    public override string Version => "1.0.0";
    public override string Description => "System home dashboard";
    public override string Author => "T-Orbit";
    public override string Icon => "HomeOutline";
    public override string Tags => "System,Home,Builtin";
}
