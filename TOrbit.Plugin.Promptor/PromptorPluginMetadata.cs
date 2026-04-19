using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Promptor;

public sealed class PromptorPluginMetadata : PluginBaseMetadata
{
    public static PromptorPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.promptor";
    public override string Name => "提示优化";
    public override string Version => "1.0.0";
    public override string Description => "提示词优化";
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
            DisplayName: "提供方",
            Description: "模型提供方",
            IsRequired: true,
            AllowedValues: ["openai", "qwen", "kimi", "ollama"]),
        new PluginVariableDefinition(
            Key: "PROMPTOR_API_ENDPOINT",
            DefaultValue: "",
            DisplayName: "接口地址",
            Description: "自定义地址"),
        new PluginVariableDefinition(
            Key: "PROMPTOR_API_KEY",
            DefaultValue: "",
            DisplayName: "接口密钥",
            Description: "鉴权密钥",
            IsEncrypted: true),
        new PluginVariableDefinition(
            Key: "PROMPTOR_MODEL_NAME",
            DefaultValue: "gpt-4o",
            DisplayName: "模型名",
            Description: "调用模型",
            IsRequired: true),
        new PluginVariableDefinition(
            Key: "PROMPTOR_MAX_TOKENS",
            DefaultValue: "2048",
            DisplayName: "最大Token",
            Description: "输出上限",
            IsRequired: true,
            ValidationPattern: @"^\d+$",
            ValidationMessage: "需为正整数"),
        new PluginVariableDefinition(
            Key: "PROMPTOR_TEMPERATURE",
            DefaultValue: "1.0",
            DisplayName: "温度",
            Description: "随机程度",
            ValidationPattern: @"^(?:0(?:\.\d+)?|1(?:\.\d+)?|2(?:\.0+)?)$",
            ValidationMessage: "需在0-2间")
    ];
}
