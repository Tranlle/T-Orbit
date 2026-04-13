using Avalonia.Styling;
using Tranbok.Tools.Designer.Abstractions;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.Services;

public sealed class BuiltInThemePaletteProvider : IThemePaletteProvider
{
    public IReadOnlyList<ThemePalette> GetPalettes() =>
    [
        new ThemePalette
        {
            Key = "tranbok-dark",
            Label = "Tranbok Dark",
            Description = "默认深色蓝调方案",
            Source = "BuiltIn",
            IsBuiltIn = true,
            BaseVariant = ThemeVariant.Dark,
            AccentBrush = "#5B8CFF",
            AccentForegroundBrush = "#FFFFFF",
            BackgroundBrush = "#0F1115",
            SurfaceBrush = "#151923",
            SurfaceElevatedBrush = "#1B2130",
            BorderBrush = "#293044",
            TextPrimaryBrush = "#F4F7FB",
            TextSecondaryBrush = "#AAB4C4",
            TextMutedBrush = "#738094",
            BadgeSuccessBackgroundBrush = "#1F2D23",
            BadgeWarningBackgroundBrush = "#322818",
            BadgeDangerBackgroundBrush = "#331C22"
        },
        new ThemePalette
        {
            Key = "tranbok-light",
            Label = "Tranbok Light",
            Description = "默认浅色清爽方案",
            BaseVariant = ThemeVariant.Light,
            AccentBrush = "#4C7CF6",
            AccentForegroundBrush = "#FFFFFF",
            BackgroundBrush = "#F6F8FC",
            SurfaceBrush = "#FFFFFF",
            SurfaceElevatedBrush = "#F0F4FA",
            BorderBrush = "#D9E1EE",
            TextPrimaryBrush = "#172033",
            TextSecondaryBrush = "#4C5870",
            TextMutedBrush = "#7A879C",
            BadgeSuccessBackgroundBrush = "#EAF8F0",
            BadgeWarningBackgroundBrush = "#FFF5E8",
            BadgeDangerBackgroundBrush = "#FDEDEE"
        },
        new ThemePalette
        {
            Key = "emerald-dark",
            Label = "Emerald Dark",
            Description = "偏绿色的深色方案",
            BaseVariant = ThemeVariant.Dark,
            AccentBrush = "#2EC4A6",
            AccentForegroundBrush = "#0D1A17",
            BackgroundBrush = "#0E1413",
            SurfaceBrush = "#131C1A",
            SurfaceElevatedBrush = "#182321",
            BorderBrush = "#27403A",
            TextPrimaryBrush = "#EDF8F5",
            TextSecondaryBrush = "#A8C7BF",
            TextMutedBrush = "#6D8B83",
            BadgeSuccessBackgroundBrush = "#1C3027",
            BadgeWarningBackgroundBrush = "#332A16",
            BadgeDangerBackgroundBrush = "#331D21"
        },
        new ThemePalette
        {
            Key = "violet-dark",
            Label = "Violet Dark",
            Description = "偏紫色的深色方案",
            BaseVariant = ThemeVariant.Dark,
            AccentBrush = "#8B7CFF",
            AccentForegroundBrush = "#FFFFFF",
            BackgroundBrush = "#100F17",
            SurfaceBrush = "#171622",
            SurfaceElevatedBrush = "#1E1C2B",
            BorderBrush = "#312D49",
            TextPrimaryBrush = "#F3F1FF",
            TextSecondaryBrush = "#B8B3D6",
            TextMutedBrush = "#7C7798",
            BadgeSuccessBackgroundBrush = "#1D2E25",
            BadgeWarningBackgroundBrush = "#322815",
            BadgeDangerBackgroundBrush = "#351D28"
        }
    ];
}