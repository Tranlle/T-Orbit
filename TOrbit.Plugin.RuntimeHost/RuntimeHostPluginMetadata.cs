using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Plugin.RuntimeHost;

public sealed class RuntimeHostPluginMetadata : PluginBaseMetadata
{
    public static RuntimeHostPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.runtime-host";
    public override string Name => ".NET Runtime Host";
    public override string Version => "1.0.0";
    public override string Description => "Deploy and manage local .NET release packages.";
    public override string Author => "T-Orbit";
    public override string Icon => "RocketLaunch";
    public override string Tags => "DotNet,Runtime,Process,Deployment";
    public override IReadOnlyList<PluginCapability> Capabilities =>
    [
        PluginCapability.FileSystem,
        PluginCapability.LocalProcess
    ];
}
