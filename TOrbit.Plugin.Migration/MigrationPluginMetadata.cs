using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Migration;

public sealed class MigrationPluginMetadata : PluginBaseMetadata
{
    public static MigrationPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.migration";
    public override string Name => "Database Migration";
    public override string Version => "1.0.1";
    public override string Description => "EF Core migration management";
    public override string Author => "T-Orbit";
    public override string Icon => "Database";
    public override string Tags => "Database,Efcore,Migration";
    public override IReadOnlyList<PluginCapability> Capabilities =>
    [
        PluginCapability.FileSystem,
        PluginCapability.LocalProcess,
        PluginCapability.Secrets
    ];

    public override IReadOnlyList<PluginVariableDefinition> VariableDefinitions =>
    [
        new PluginVariableDefinition(
            Key: "TORBIT_DB_CONNECTION",
            DefaultValue: "",
            DisplayName: "Database Connection",
            Description: "Default connection string for migration commands",
            IsRequired: true)
    ];
}
