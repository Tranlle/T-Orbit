using Tranbok.Tools.Designer.Abstractions;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.Services;

public sealed class ThemePaletteRegistry
{
    private readonly IReadOnlyList<IThemePaletteProvider> _providers;

    public ThemePaletteRegistry(IEnumerable<IThemePaletteProvider> providers)
    {
        _providers = providers.ToList();
    }

    public IReadOnlyList<ThemePalette> GetAll()
    {
        return _providers
            .SelectMany(provider => provider.GetPalettes())
            .GroupBy(palette => palette.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    public ThemePalette? Find(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return GetAll().FirstOrDefault(palette => string.Equals(palette.Key, key, StringComparison.OrdinalIgnoreCase));
    }
}