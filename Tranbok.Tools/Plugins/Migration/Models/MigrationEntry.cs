namespace Tranbok.Tools.Plugins.Migration.Models;

public enum MigrationStatus
{
    Unknown,   // DB not connected or status not fetched
    Applied,   // Present in __EFMigrationsHistory
    Pending    // File exists but not applied
}

public sealed class MigrationEntry : Infrastructure.ObservableObject
{
    private MigrationStatus _status = MigrationStatus.Unknown;
    private string? _content;

    /// <summary>Timestamp prefix, e.g. "20240101000000"</summary>
    public string TimestampId { get; init; } = string.Empty;

    /// <summary>Human-readable name, e.g. "InitialCreate"</summary>
    public string MigrationName { get; init; } = string.Empty;

    /// <summary>Full name as used by EF, e.g. "20240101000000_InitialCreate"</summary>
    public string FullName => $"{TimestampId}_{MigrationName}";

    /// <summary>Absolute path to the .cs migration file</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Absolute path to the .Designer.cs file (may not exist)</summary>
    public string DesignerPath => System.IO.Path.ChangeExtension(FilePath, null) + ".Designer.cs";

    public MigrationStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    /// <summary>Lazy-loaded file content</summary>
    public string? Content
    {
        get => _content;
        set => SetField(ref _content, value);
    }

    /// <summary>True when this is the last migration in the list (set by ViewModel after list refresh).</summary>
    private bool _isLast;
    public bool IsLast
    {
        get => _isLast;
        set => SetField(ref _isLast, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public DateTime CreatedAt { get; init; }

    public string StatusLabel => Status switch
    {
        MigrationStatus.Applied => "Applied",
        MigrationStatus.Pending => "Pending",
        _                       => "Unknown"
    };

    public string FormattedDate => CreatedAt != default
        ? CreatedAt.ToString("yyyy-MM-dd HH:mm")
        : string.Empty;
}
