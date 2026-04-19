using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using TOrbit.Designer.Services;
using TOrbit.Designer.ViewModels;

namespace TOrbit.Plugin.Migration.Models;

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
    public string DbTypeTag => DbType switch
    {
        DbType.SqlServer => "SqlServer",
        DbType.PostgreSQL => "PgSql",
        DbType.MySQL => "MySql",
        _ => DbType.ToString()
    };
    public bool IsReady => !string.IsNullOrWhiteSpace(ConnectionString) && !string.IsNullOrWhiteSpace(ContextName);
    public string ReadyStatusText => IsReady
        ? (LocalizationService.Current?.GetString("migration.profile.ready") ?? "Ready")
        : (LocalizationService.Current?.GetString("migration.profile.needsConfig") ?? "Needs Config");
    public IBrush ReadyBadgeBackground => ResolveBrush(IsReady ? "TOrbitBadgeSuccessBackgroundBrush" : "TOrbitBadgeDangerBackgroundBrush");
    public IBrush ReadyBadgeForeground => ResolveBrush(IsReady ? "TOrbitBadgeSuccessForegroundBrush" : "TOrbitBadgeDangerForegroundBrush");

    partial void OnConnectionStringChanged(string value) => RaiseComputedProperties();
    partial void OnContextNameChanged(string value) => RaiseComputedProperties();
    partial void OnDbTypeChanged(DbType value) => RaiseComputedProperties();
    partial void OnProfileNameChanged(string value) => OnPropertyChanged(nameof(EffectiveProfileName));

    private void RaiseComputedProperties()
    {
        OnPropertyChanged(nameof(DbTypeTag));
        OnPropertyChanged(nameof(IsReady));
        OnPropertyChanged(nameof(ReadyStatusText));
        OnPropertyChanged(nameof(ReadyBadgeBackground));
        OnPropertyChanged(nameof(ReadyBadgeForeground));
    }

    public void NotifyLocalizationChanged() => RaiseComputedProperties();

    private static IBrush ResolveBrush(string resourceKey)
        => Application.Current?.TryGetResource(resourceKey, Application.Current.ActualThemeVariant, out var value) == true && value is IBrush brush
            ? brush
            : Brushes.Transparent;
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
