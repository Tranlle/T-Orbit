using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Tranbok.Tools.Designer.Converters;

public sealed class NullToBoolInverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.IEnumerable enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            return !enumerator.MoveNext();
        }

        return value is null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
