using System.Text.RegularExpressions;

namespace TOrbit.Plugin.Core.Models;

public static class PluginVariableValidator
{
    public static IReadOnlyList<string> Validate(
        string pluginName,
        IReadOnlyList<PluginVariableDefinition> definitions,
        IReadOnlyDictionary<string, string> values)
    {
        var errors = new List<string>();

        foreach (var definition in definitions)
        {
            values.TryGetValue(definition.Key, out var value);
            var normalizedValue = value?.Trim() ?? string.Empty;
            var displayName = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.Key : definition.DisplayName;

            if (definition.IsRequired && string.IsNullOrWhiteSpace(normalizedValue))
            {
                errors.Add($"{pluginName}: {displayName} 为必填项。");
                continue;
            }

            if (string.IsNullOrWhiteSpace(normalizedValue))
                continue;

            if (definition.AllowedValues is { Count: > 0 }
                && !definition.AllowedValues.Contains(normalizedValue, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"{pluginName}: {displayName} 必须为 {string.Join(" / ", definition.AllowedValues)} 之一。");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(definition.ValidationPattern)
                && !Regex.IsMatch(normalizedValue, definition.ValidationPattern, RegexOptions.CultureInvariant))
            {
                errors.Add(
                    $"{pluginName}: {displayName} {definition.ValidationMessage ?? "格式不正确。"}");
            }
        }

        return errors;
    }
}
