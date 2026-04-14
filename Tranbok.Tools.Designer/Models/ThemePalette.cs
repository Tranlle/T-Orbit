using Avalonia.Styling;

namespace Tranbok.Tools.Designer.Models;

public sealed class ThemePalette
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = "BuiltIn";
    public bool IsBuiltIn { get; set; } = true;
    public ThemeVariant BaseVariant { get; set; } = ThemeVariant.Dark;

    // Core accent
    public string AccentBrush { get; set; } = "#5B8CFF";
    public string AccentForegroundBrush { get; set; } = "#FFFFFF";

    // Accent subtle — used for selected item backgrounds, hover tints
    public string AccentSubtleBrush { get; set; } = "#1D273D";
    public string AccentSubtleForegroundBrush { get; set; } = "#8AADFF";

    // Surfaces
    public string BackgroundBrush { get; set; } = "#0F1115";
    public string SurfaceBrush { get; set; } = "#151923";
    public string SurfaceElevatedBrush { get; set; } = "#1B2130";
    public string BorderBrush { get; set; } = "#293044";

    // Text
    public string TextPrimaryBrush { get; set; } = "#F4F7FB";
    public string TextSecondaryBrush { get; set; } = "#AAB4C4";
    public string TextMutedBrush { get; set; } = "#738094";

    // Semantic badge colors — background + foreground pairs
    public string BadgeSuccessBackgroundBrush { get; set; } = "#1F2D23";
    public string BadgeSuccessForegroundBrush { get; set; } = "#5ADE97";
    public string BadgeWarningBackgroundBrush { get; set; } = "#322818";
    public string BadgeWarningForegroundBrush { get; set; } = "#F0C878";
    public string BadgeDangerBackgroundBrush { get; set; } = "#331C22";
    public string BadgeDangerForegroundBrush { get; set; } = "#F47482";
}
