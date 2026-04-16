using TOrbit.Plugin.Core.Models;
using Xunit;

namespace TOrbit.Core.Tests.Models;

public sealed class PluginVariableValidatorTests
{
    [Fact]
    public void Validate_ReturnsErrorsForRequiredAllowedAndPatternRules()
    {
        PluginVariableDefinition[] definitions =
        [
            new PluginVariableDefinition(
                Key: "PROVIDER",
                DefaultValue: "openai",
                DisplayName: "提供方",
                IsRequired: true,
                AllowedValues: ["openai", "ollama"]),
            new PluginVariableDefinition(
                Key: "MAX_TOKENS",
                DefaultValue: "2048",
                DisplayName: "最大 Token 数",
                ValidationPattern: @"^\d+$",
                ValidationMessage: "必须是正整数。"),
            new PluginVariableDefinition(
                Key: "API_KEY",
                DefaultValue: "",
                DisplayName: "API Key",
                IsRequired: true)
        ];

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PROVIDER"] = "invalid",
            ["MAX_TOKENS"] = "abc",
            ["API_KEY"] = ""
        };

        var errors = PluginVariableValidator.Validate("Promptor", definitions, values);

        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, item => item.Contains("提供方", StringComparison.Ordinal));
        Assert.Contains(errors, item => item.Contains("最大 Token 数", StringComparison.Ordinal));
        Assert.Contains(errors, item => item.Contains("API Key", StringComparison.Ordinal));
    }
}
