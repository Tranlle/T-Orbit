using CommunityToolkit.Mvvm.ComponentModel;
using Tranbok.Tools.Designer.ViewModels;

namespace Tranbok.Tools.Plugin.Migration.Models;

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
        MigrationStatus.Applied => "Applied",
        MigrationStatus.Pending => "Pending",
        _ => "Unknown"
    };

    public string FormattedDate => CreatedAt != default
        ? CreatedAt.ToString("yyyy-MM-dd HH:mm")
        : string.Empty;
}
