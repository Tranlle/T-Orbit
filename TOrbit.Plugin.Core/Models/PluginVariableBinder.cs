using System.Globalization;
using System.Reflection;

namespace TOrbit.Plugin.Core.Models;

public static class PluginVariableBinder
{
    public static T Bind<T>(IReadOnlyDictionary<string, string> values) where T : new()
    {
        var instance = new T();
        var map = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
                continue;

            var key = property.GetCustomAttribute<PluginVariableKeyAttribute>()?.Key ?? property.Name;
            if (!map.TryGetValue(key, out var rawValue))
                continue;

            if (TryConvert(rawValue, property.PropertyType, out var converted))
                property.SetValue(instance, converted);
        }

        return instance;
    }

    private static bool TryConvert(string rawValue, Type targetType, out object? converted)
    {
        var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (effectiveType == typeof(string))
        {
            converted = rawValue;
            return true;
        }

        if (effectiveType.IsEnum && Enum.TryParse(effectiveType, rawValue, true, out var enumValue))
        {
            converted = enumValue;
            return true;
        }

        try
        {
            converted = Convert.ChangeType(rawValue, effectiveType, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            converted = null;
            return false;
        }
    }
}
