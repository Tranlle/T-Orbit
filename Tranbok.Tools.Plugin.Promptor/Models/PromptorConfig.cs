namespace Tranbok.Tools.Plugin.Promptor.Models;

public sealed record PromptorConfig(
    string Provider,
    string ApiEndpoint,
    string ApiKey,
    string ModelName,
    int MaxTokens,
    double Temperature
);
