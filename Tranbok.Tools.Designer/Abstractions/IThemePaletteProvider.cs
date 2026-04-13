using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.Abstractions;

public interface IThemePaletteProvider
{
    IReadOnlyList<ThemePalette> GetPalettes();
}