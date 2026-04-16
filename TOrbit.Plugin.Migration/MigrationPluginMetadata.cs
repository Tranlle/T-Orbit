using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Migration;

public sealed class MigrationPluginMetadata : PluginBaseMetadata
{
    public static MigrationPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.migration";
    public override string Name => "数据库迁移";
    public override string Version => "1.0.1";
    public override string Description => "管理 EF Core 迁移文件：新增、编辑、执行、回滚，支持 SqlServer / PostgreSQL / MySQL。";
    public override string Author => "T-Orbit";
    public override string Icon => "Database";
    public override string Tags => "database,efcore,migration";
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
            DisplayName: "数据库连接字符串",
            Description: "执行迁移时传递给 dotnet ef 命令的数据库连接字符串。",
            IsRequired: true)
    ];
}
