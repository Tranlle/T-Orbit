using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.Services;

public sealed class ThemeService : IThemeService
{
    private readonly ThemePaletteRegistry _registry;
    private string _currentPaletteKey;

    public ThemeService(ThemePaletteRegistry registry)
    {
        _registry = registry;
        _currentPaletteKey = registry.GetAll().FirstOrDefault()?.Key ?? "tranbok-dark";
    }

    public ThemeVariant CurrentTheme => Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
    public string CurrentPaletteKey => _currentPaletteKey;

    public IReadOnlyList<ThemePalette> GetAvailablePalettes() => _registry.GetAll();

    public void SetTheme(ThemeVariant themeVariant)
    {
        var palette = _registry.Find(_currentPaletteKey) ?? _registry.GetAll().FirstOrDefault();
        if (palette is null)
        {
            return;
        }

        palette.BaseVariant = themeVariant;
        ApplyPalette(palette);
    }

    public void SetTheme(string themeName)
    {
        var themeVariant = themeName.Trim().ToLowerInvariant() switch
        {
            "light" => ThemeVariant.Light,
            "dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        SetTheme(themeVariant);
    }

    public void SetPalette(string paletteKey)
    {
        var palette = _registry.Find(paletteKey) ?? _registry.GetAll().FirstOrDefault();
        if (palette is null)
        {
            return;
        }

        _currentPaletteKey = palette.Key;
        ApplyPalette(palette);
    }

    public void ApplyTheme(string paletteKey)
    {
        SetPalette(paletteKey);
    }

    private static void ApplyBrush(string key, string color)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.Resources[key] = new SolidColorBrush(Color.Parse(color));
    }

    private void ApplyPalette(ThemePalette palette)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = palette.BaseVariant;
        _currentPaletteKey = palette.Key;

        ApplyBrush("TranbokAccentBrush", palette.AccentBrush);
        ApplyBrush("TranbokAccentForegroundBrush", palette.AccentForegroundBrush);
        ApplyBrush("TranbokBackgroundBrush", palette.BackgroundBrush);
        ApplyBrush("TranbokSurfaceBrush", palette.SurfaceBrush);
        ApplyBrush("TranbokSurfaceElevatedBrush", palette.SurfaceElevatedBrush);
        ApplyBrush("TranbokBorderBrush", palette.BorderBrush);
        ApplyBrush("TranbokTextPrimaryBrush", palette.TextPrimaryBrush);
        ApplyBrush("TranbokTextSecondaryBrush", palette.TextSecondaryBrush);
        ApplyBrush("TranbokTextMutedBrush", palette.TextMutedBrush);
        ApplyBrush("TranbokBadgeSuccessBackgroundBrush", palette.BadgeSuccessBackgroundBrush);
        ApplyBrush("TranbokBadgeWarningBackgroundBrush", palette.BadgeWarningBackgroundBrush);
        ApplyBrush("TranbokBadgeDangerBackgroundBrush", palette.BadgeDangerBackgroundBrush);
    }
}