using Avalonia.Styling;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Models;

namespace TOrbit.Designer.Services;

public sealed class BuiltInThemePaletteProvider : IThemePaletteProvider
{
    public IReadOnlyList<ThemePalette> GetPalettes() =>
    [
        new ThemePalette
        {
            Key = "tranbok-dark",
            Label = "T-Orbit Dark",
            Description = L("settings.palettes.tranbok-dark.description", "Default deep blue"),
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
            SurfaceHoverBrush = "#212C40",
            BorderBrush = "#293044",
            BorderHoverBrush = "#3A5070",
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
            Label = "T-Orbit Light",
            Description = L("settings.palettes.tranbok-light.description", "Default light palette"),
            BaseVariant = ThemeVariant.Light,
            AccentBrush = "#5B72D6",
            AccentForegroundBrush = "#FFFFFF",
            AccentSubtleBrush = "#E8ECFA",
            AccentSubtleForegroundBrush = "#3D55C0",
            BackgroundBrush = "#EDEFF4",
            SurfaceBrush = "#F5F7FB",
            SurfaceElevatedBrush = "#E7EBF3",
            SurfaceHoverBrush = "#D6DCE8",
            BorderBrush = "#C8D1DE",
            BorderHoverBrush = "#8A9BB0",
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
            Description = L("settings.palettes.emerald-dark.description", "Dark palette with emerald accent"),
            BaseVariant = ThemeVariant.Dark,
            AccentBrush = "#2EC4A6",
            AccentForegroundBrush = "#0D1A17",
            AccentSubtleBrush = "#163028",
            AccentSubtleForegroundBrush = "#4ED4BC",
            BackgroundBrush = "#0E1413",
            SurfaceBrush = "#131C1A",
            SurfaceElevatedBrush = "#182321",
            SurfaceHoverBrush = "#1E2D2A",
            BorderBrush = "#27403A",
            BorderHoverBrush = "#306050",
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
            Description = L("settings.palettes.violet-dark.description", "Dark palette with violet accent"),
            BaseVariant = ThemeVariant.Dark,
            AccentBrush = "#8B7CFF",
            AccentForegroundBrush = "#FFFFFF",
            AccentSubtleBrush = "#22203A",
            AccentSubtleForegroundBrush = "#A99EFF",
            BackgroundBrush = "#100F17",
            SurfaceBrush = "#171622",
            SurfaceElevatedBrush = "#1E1C2B",
            SurfaceHoverBrush = "#252339",
            BorderBrush = "#312D49",
            BorderHoverBrush = "#4A4570",
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

    private static string L(string key, string fallback)
        => LocalizationService.Current?.GetString(key) ?? fallback;
}
