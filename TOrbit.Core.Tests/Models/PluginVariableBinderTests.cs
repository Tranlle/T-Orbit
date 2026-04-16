using TOrbit.Plugin.Core.Models;
using Xunit;

namespace TOrbit.Core.Tests.Models;

public sealed class PluginVariableBinderTests
{
    [Fact]
    public void Bind_BindsTypedPropertiesFromDictionary()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PROMPTOR_PROVIDER"] = "openai",
            ["PROMPTOR_MAX_TOKENS"] = "4096",
            ["PROMPTOR_TEMPERATURE"] = "0.7",
            ["IsEnabled"] = "true"
        };

        var result = PluginVariableBinder.Bind<TestVariables>(values);

        Assert.Equal("openai", result.Provider);
        Assert.Equal(4096, result.MaxTokens);
        Assert.Equal(0.7, result.Temperature, 3);
        Assert.True(result.IsEnabled);
    }

    private sealed class TestVariables
    {
        [PluginVariableKey("PROMPTOR_PROVIDER")]
        public string Provider { get; set; } = string.Empty;

        [PluginVariableKey("PROMPTOR_MAX_TOKENS")]
        public int MaxTokens { get; set; }

        [PluginVariableKey("PROMPTOR_TEMPERATURE")]
        public double Temperature { get; set; }

        public bool IsEnabled { get; set; }
    }
}
