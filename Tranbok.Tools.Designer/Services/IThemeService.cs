using Avalonia.Media;
using Avalonia.Styling;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.Services;

public interface IThemeService
{
    ThemeVariant CurrentTheme { get; }
    string CurrentPaletteKey { get; }
    string CurrentFontOptionKey { get; }

    IReadOnlyList<ThemePalette> GetAvailablePalettes();

    void SetTheme(ThemeVariant themeVariant);
    void SetTheme(string themeName);
    void SetPalette(string paletteKey);
    void SetFontOption(string fontOptionKey);
    FontFamily ResolveFontFamily(string fontOptionKey);
    void ApplyTheme(string paletteKey);
}