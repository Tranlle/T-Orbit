using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace TOrbit.Plugin.Monitor.Converters;

public sealed class IconNameToMaterialIconKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string iconName || string.IsNullOrWhiteSpace(iconName))
            return MaterialIconKind.PuzzleOutline;

        return Enum.TryParse<MaterialIconKind>(iconName, true, out var iconKind)
            ? iconKind
            : MaterialIconKind.PuzzleOutline;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
