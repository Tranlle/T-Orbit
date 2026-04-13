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

    public string AccentBrush { get; set; } = "#5B8CFF";
    public string AccentForegroundBrush { get; set; } = "#FFFFFF";

    public string BackgroundBrush { get; set; } = "#0F1115";
    public string SurfaceBrush { get; set; } = "#151923";
    public string SurfaceElevatedBrush { get; set; } = "#1B2130";
    public string BorderBrush { get; set; } = "#293044";
    public string TextPrimaryBrush { get; set; } = "#F4F7FB";
    public string TextSecondaryBrush { get; set; } = "#AAB4C4";
    public string TextMutedBrush { get; set; } = "#738094";

    public string BadgeSuccessBackgroundBrush { get; set; } = "#1F2D23";
    public string BadgeWarningBackgroundBrush { get; set; } = "#322818";
    public string BadgeDangerBackgroundBrush { get; set; } = "#331C22";
}