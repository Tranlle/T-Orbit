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
            AccentSubtleBrush = "#1D273D",
            AccentSubtleForegroundBrush = "#8AADFF",
            BackgroundBrush = "#0F1115",
            SurfaceBrush = "#151923",
            SurfaceElevatedBrush = "#1B2130",
            BorderBrush = "#293044",
            TextPrimaryBrush = "#F4F7FB",
            TextSecondaryBrush = "#AAB4C4",
            TextMutedBrush = "#738094",
            BadgeSuccessBackgroundBrush = "#1F2D23",
            BadgeSuccessForegroundBrush = "#5ADE97",
            BadgeWarningBackgroundBrush = "#322818",
            BadgeWarningForegroundBrush = "#F0C878",
            BadgeDangerBackgroundBrush = "#331C22",
            BadgeDangerForegroundBrush = "#F47482"
        },
        new ThemePalette
        {
            Key = "tranbok-light",
            Label = "Tranbok Light",
            Description = "默认浅色清爽方案",
            BaseVariant = ThemeVariant.Light,
            AccentBrush = "#5B72D6",
            AccentForegroundBrush = "#FFFFFF",
            AccentSubtleBrush = "#E8ECFA",
            AccentSubtleForegroundBrush = "#3D55C0",
            BackgroundBrush = "#EDEFF4",
            SurfaceBrush = "#F5F7FB",
            SurfaceElevatedBrush = "#E7EBF3",
            BorderBrush = "#C8D1DE",
            TextPrimaryBrush = "#1E293B",
            TextSecondaryBrush = "#475569",
            TextMutedBrush = "#64748B",
            BadgeSuccessBackgroundBrush = "#DDEFE4",
            BadgeSuccessForegroundBrush = "#1A7A42",
            BadgeWarningBackgroundBrush = "#F6EAD8",
            BadgeWarningForegroundBrush = "#8A5010",
            BadgeDangerBackgroundBrush = "#F4DEE2",
            BadgeDangerForegroundBrush = "#B52438"
        },
        new ThemePalette
        {
            Key = "emerald-dark",
            Label = "Emerald Dark",
            Description = "偏绿色的深色方案",
            BaseVariant = ThemeVariant.Dark,
            AccentBrush = "#2EC4A6",
            AccentForegroundBrush = "#0D1A17",
            AccentSubtleBrush = "#163028",
            AccentSubtleForegroundBrush = "#4ED4BC",
            BackgroundBrush = "#0E1413",
            SurfaceBrush = "#131C1A",
            SurfaceElevatedBrush = "#182321",
            BorderBrush = "#27403A",
            TextPrimaryBrush = "#EDF8F5",
            TextSecondaryBrush = "#A8C7BF",
            TextMutedBrush = "#6D8B83",
            BadgeSuccessBackgroundBrush = "#1C3027",
            BadgeSuccessForegroundBrush = "#4ED4BC",
            BadgeWarningBackgroundBrush = "#332A16",
            BadgeWarningForegroundBrush = "#E8C070",
            BadgeDangerBackgroundBrush = "#331D21",
            BadgeDangerForegroundBrush = "#F07282"
        },
        new ThemePalette
        {
            Key = "violet-dark",
            Label = "Violet Dark",
            Description = "偏紫色的深色方案",
            BaseVariant = ThemeVariant.Dark,
            AccentBrush = "#8B7CFF",
            AccentForegroundBrush = "#FFFFFF",
            AccentSubtleBrush = "#22203A",
            AccentSubtleForegroundBrush = "#A99EFF",
            BackgroundBrush = "#100F17",
            SurfaceBrush = "#171622",
            SurfaceElevatedBrush = "#1E1C2B",
            BorderBrush = "#312D49",
            TextPrimaryBrush = "#F3F1FF",
            TextSecondaryBrush = "#B8B3D6",
            TextMutedBrush = "#7C7798",
            BadgeSuccessBackgroundBrush = "#1D2E25",
            BadgeSuccessForegroundBrush = "#5ADE97",
            BadgeWarningBackgroundBrush = "#322815",
            BadgeWarningForegroundBrush = "#F0C878",
            BadgeDangerBackgroundBrush = "#351D28",
            BadgeDangerForegroundBrush = "#F47482"
        }
    ];
}
