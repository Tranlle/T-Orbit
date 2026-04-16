using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Promptor;

public sealed class PromptorPluginMetadata : PluginBaseMetadata
{
    public static PromptorPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.promptor";
    public override string Name => "提示词优化";
    public override string Version => "1.0.0";
    public override string Description => "基于大模型的提示词优化工具，支持结构化、少样本、思维链等多种优化策略。";
    public override string Author => "T-Orbit";
    public override string Icon => "AutoFixHigh";
    public override string Tags => "ai,prompt,llm";
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
            Description: "AI 提供商，支持 openai / qwen / kimi / ollama；填写后可自动推断默认端点。",
            IsRequired: true,
            AllowedValues: ["openai", "qwen", "kimi", "ollama"]),
        new PluginVariableDefinition(
            Key: "PROMPTOR_API_ENDPOINT",
            DefaultValue: "",
            DisplayName: "API 端点",
            Description: "API Base URL；留空时根据提供方自动推断，例如 https://api.openai.com/v1。"),
        new PluginVariableDefinition(
            Key: "PROMPTOR_API_KEY",
            DefaultValue: "",
            DisplayName: "API Key",
            Description: "用于鉴权的 API Key；Ollama 等本地服务可留空。",
            IsEncrypted: true),
        new PluginVariableDefinition(
            Key: "PROMPTOR_MODEL_NAME",
            DefaultValue: "gpt-4o",
            DisplayName: "模型名称",
            Description: "调用的模型 ID，例如 gpt-4o、qwen-plus、moonshot-v1-8k。",
            IsRequired: true),
        new PluginVariableDefinition(
            Key: "PROMPTOR_MAX_TOKENS",
            DefaultValue: "2048",
            DisplayName: "最大 Token 数",
            Description: "单次生成的最大 Token 上限，默认 2048。",
            IsRequired: true,
            ValidationPattern: @"^\d+$",
            ValidationMessage: "必须是正整数。"),
        new PluginVariableDefinition(
            Key: "PROMPTOR_TEMPERATURE",
            DefaultValue: "1.0",
            DisplayName: "温度参数",
            Description: "生成随机性，范围建议 0.0-2.0，值越高越有创造性，默认 1.0。",
            ValidationPattern: @"^(?:0(?:\.\d+)?|1(?:\.\d+)?|2(?:\.0+)?)$",
            ValidationMessage: "必须是 0.0 到 2.0 之间的数字。")
    ];
}
