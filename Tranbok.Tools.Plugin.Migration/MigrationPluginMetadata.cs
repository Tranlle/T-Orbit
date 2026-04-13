using Tranbok.Tools.Plugin.Core.Base;

namespace Tranbok.Tools.Plugin.Migration;

public sealed class MigrationPluginMetadata : PluginBaseMetadata
{
    public static MigrationPluginMetadata Instance { get; } = new();

    public override string Id => "migration";

    public override string Name => "数据库迁移";

    public override string Version => "1.0.0";

    public override string Description => "管理 EF Core 迁移文件：新增、编辑、执行、撤回，支持 SqlServer / PostgreSQL / MySQL";

    public override string Author => "Tranbok";

    public override string Icon => "database";
}
