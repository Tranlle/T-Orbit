using System.Text.Json.Serialization;

namespace Tranbok.Tools.Plugins.Migration.Models;

public enum DbType
{
    SqlServer,
    PostgreSQL,
    MySQL
}

public sealed class DbConnectionProfile : Infrastructure.ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _profileName = string.Empty;
    private string _connectionString = string.Empty;
    private string _contextName = string.Empty;
    private bool _useWorkspace = true;
    private bool _isSelected;
    private DbType _dbType = DbType.SqlServer;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public DbType DbType
    {
        get => _dbType;
        set => SetField(ref _dbType, value);
    }

    public string FolderName => DbType switch
    {
        DbType.SqlServer  => "SqlServer",
        DbType.PostgreSQL => "Pgsql",
        DbType.MySQL      => "Mysql",
        _                 => "SqlServer"
    };

    public string DisplayName => DbType switch
    {
        DbType.SqlServer  => "SQL Server",
        DbType.PostgreSQL => "PostgreSQL",
        DbType.MySQL      => "MySQL",
        _                 => DbType.ToString()
    };

    public string ProfileName
    {
        get => string.IsNullOrWhiteSpace(_profileName) ? DisplayName : _profileName;
        set => SetField(ref _profileName, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public string ConnectionString
    {
        get => _connectionString;
        set => SetField(ref _connectionString, value);
    }

    /// <summary>Optional DbContext class name for --context flag. Leave empty to omit.</summary>
    public string ContextName
    {
        get => _contextName;
        set => SetField(ref _contextName, value);
    }

    public bool UseWorkspace
    {
        get => _useWorkspace;
        set => SetField(ref _useWorkspace, value);
    }
}

/// <summary>Persisted tool configuration for a project, saved alongside the project.</summary>
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
    public string ProviderProjectPath { get; set; } = string.Empty;
}
