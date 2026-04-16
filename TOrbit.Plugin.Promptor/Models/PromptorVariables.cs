using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Promptor.Models;

public sealed class PromptorVariables
{
    [PluginVariableKey("PROMPTOR_PROVIDER")]
    public string Provider { get; set; } = "openai";

    [PluginVariableKey("PROMPTOR_API_ENDPOINT")]
    public string ApiEndpoint { get; set; } = string.Empty;

    [PluginVariableKey("PROMPTOR_API_KEY")]
    public string ApiKey { get; set; } = string.Empty;

    [PluginVariableKey("PROMPTOR_MODEL_NAME")]
    public string ModelName { get; set; } = "gpt-4o";

    [PluginVariableKey("PROMPTOR_MAX_TOKENS")]
    public int MaxTokens { get; set; } = 2048;

    [PluginVariableKey("PROMPTOR_TEMPERATURE")]
    public double Temperature { get; set; } = 1.0;
}
