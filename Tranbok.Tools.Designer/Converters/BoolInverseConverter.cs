using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Tranbok.Tools.Designer.Converters;

public sealed class BoolInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag ? !flag : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag ? !flag : value;
}
