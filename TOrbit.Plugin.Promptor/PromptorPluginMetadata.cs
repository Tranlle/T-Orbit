using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Promptor;

public sealed class PromptorPluginMetadata : PluginBaseMetadata
{
    public static PromptorPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.promptor";
    public override string Name => "Promptor";
    public override string Version => "1.0.0";
    public override string Description => "Prompt optimization workspace";
    public override string Author => "T-Orbit";
    public override string Icon => "AutoFixHigh";
    public override string Tags => "AI,Prompt,Llm";
    public override IReadOnlyList<PluginCapability> Capabilities =>
    [
        PluginCapability.Network,
        PluginCapability.Secrets
    ];

    public override IReadOnlyList<PluginVariableDefinition> VariableDefinitions =>
    [
        new PluginVariableDefinition(
            Key: "PROMPTOR_PROVIDER",
            DefaultValue: "openai",
            DisplayName: "Provider",
            Description: "Model provider",
            IsRequired: true,
            AllowedValues: ["openai", "qwen", "kimi", "ollama"]),
        new PluginVariableDefinition(
            Key: "PROMPTOR_API_ENDPOINT",
            DefaultValue: "",
            DisplayName: "API Endpoint",
            Description: "Custom API endpoint"),
        new PluginVariableDefinition(
            Key: "PROMPTOR_API_KEY",
            DefaultValue: "",
            DisplayName: "API Key",
            Description: "Authorization credential",
            IsEncrypted: true),
        new PluginVariableDefinition(
            Key: "PROMPTOR_MODEL_NAME",
            DefaultValue: "gpt-4o",
            DisplayName: "Model Name",
            Description: "Target model",
            IsRequired: true),
        new PluginVariableDefinition(
            Key: "PROMPTOR_MAX_TOKENS",
            DefaultValue: "2048",
            DisplayName: "Max Tokens",
            Description: "Maximum output tokens",
            IsRequired: true,
            ValidationPattern: @"^\d+$",
            ValidationMessage: "Must be a positive integer"),
        new PluginVariableDefinition(
            Key: "PROMPTOR_TEMPERATURE",
            DefaultValue: "1.0",
            DisplayName: "Temperature",
            Description: "Sampling temperature",
            ValidationPattern: @"^(?:0(?:\.\d+)?|1(?:\.\d+)?|2(?:\.0+)?)$",
            ValidationMessage: "Must be between 0 and 2")
    ];
}
