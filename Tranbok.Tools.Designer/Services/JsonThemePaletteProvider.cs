using System.Text.Json;
using Tranbok.Tools.Designer.Abstractions;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.Services;

public sealed class JsonThemePaletteProvider : IThemePaletteProvider
{
    private readonly string _themeDirectory;

    public JsonThemePaletteProvider(string themeDirectory)
    {
        _themeDirectory = themeDirectory;
    }

    public IReadOnlyList<ThemePalette> GetPalettes()
    {
        if (!Directory.Exists(_themeDirectory))
        {
            return [];
        }

        var result = new List<ThemePalette>();

        foreach (var file in Directory.EnumerateFiles(_themeDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var json = File.ReadAllText(file);
                var palette = JsonSerializer.Deserialize<ThemePalette>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (palette is null || string.IsNullOrWhiteSpace(palette.Key))
                {
                    continue;
                }

                palette.Source = "Json";
                palette.IsBuiltIn = false;
                result.Add(palette);
            }
            catch
            {
            }
        }

        return result;
    }
}