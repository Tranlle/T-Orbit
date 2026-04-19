using CommunityToolkit.Mvvm.ComponentModel;
using TOrbit.Designer.Services;
using TOrbit.Designer.ViewModels;

namespace TOrbit.Plugin.Migration.Models;

public enum MigrationStatus
{
    Unknown,
    Applied,
    Pending
}

public sealed partial class MigrationEntry : PluginBaseViewModel
{
    [ObservableProperty]
    private MigrationStatus status = MigrationStatus.Unknown;

    [ObservableProperty]
    private string? content;

    [ObservableProperty]
    private bool isLast;

    [ObservableProperty]
    private bool isSelected;

    public string TimestampId { get; init; } = string.Empty;
    public string MigrationName { get; init; } = string.Empty;
    public string FullName => $"{TimestampId}_{MigrationName}";
    public string FilePath { get; set; } = string.Empty;
    public string DesignerPath => Path.ChangeExtension(FilePath, null) + ".Designer.cs";
    public DateTime CreatedAt { get; init; }

    public string StatusLabel => Status switch
    {
        MigrationStatus.Applied => LocalizationService.Current?.GetString("migration.status.applied") ?? "Applied",
        MigrationStatus.Pending => LocalizationService.Current?.GetString("migration.status.pending") ?? "Pending",
        _ => LocalizationService.Current?.GetString("migration.status.unknown") ?? "Unknown"
    };

    public string FormattedDate => CreatedAt != default
        ? CreatedAt.ToString("yyyy-MM-dd HH:mm")
        : string.Empty;

    public void NotifyLocalizationChanged() => OnPropertyChanged(nameof(StatusLabel));
}
