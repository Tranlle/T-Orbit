using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using Tranbok.Tools.Designer.ViewModels;

namespace Tranbok.Tools.Plugin.Migration.Models;

public enum DbType
{
    SqlServer,
    PostgreSQL,
    MySQL
}

public sealed partial class DbConnectionProfile : PluginBaseViewModel
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string profileName = string.Empty;

    [ObservableProperty]
    private string connectionString = string.Empty;

    [ObservableProperty]
    private string contextName = string.Empty;

    [ObservableProperty]
    private bool useWorkspace = true;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private DbType dbType = DbType.SqlServer;

    public string FolderName => DbType switch
    {
        DbType.SqlServer => "SqlServer",
        DbType.PostgreSQL => "Pgsql",
        DbType.MySQL => "Mysql",
        _ => "SqlServer"
    };

    public string DisplayName => DbType switch
    {
        DbType.SqlServer => "SQL Server",
        DbType.PostgreSQL => "PostgreSQL",
        DbType.MySQL => "MySQL",
        _ => DbType.ToString()
    };

    public string EffectiveProfileName => string.IsNullOrWhiteSpace(ProfileName) ? DisplayName : ProfileName;
}

public sealed class MigrationToolConfig
{
    public string ProjectPath { get; set; } = string.Empty;
    public string? ActiveProfileId { get; set; }
    public List<MigrationProfileSettings> Profiles { get; set; } = [];
}

public sealed class MigrationSettings
{
    public string ProjectPath { get; set; } = string.Empty;
    public string? ActiveProfileId { get; set; }
    public List<MigrationProfileSettings> Profiles { get; set; } = [];
}

public sealed class MigrationProfileSettings
{
    public string Id { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DbType DbType { get; set; } = DbType.SqlServer;

    public string ContextName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool UseWorkspace { get; set; } = true;
}

public sealed class WorkspaceInfo
{
    public string WorkspaceDirectory { get; set; } = string.Empty;
    public string WorkspaceProjectPath { get; set; } = string.Empty;
    public string StartupProjectPath { get; set; } = string.Empty;
    public string RelativeOutputDirectory { get; set; } = string.Empty;
    public string DomainProjectPath { get; set; } = string.Empty;
}
