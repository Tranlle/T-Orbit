namespace TOrbit.Plugin.Core.Models;

/// <summary>
/// 插件在元数据中声明的环境变量定义，包含键名、默认值和展示信息。
/// 具体的校验与错误处理由插件自行负责。
/// </summary>
public sealed record PluginVariableDefinition(
    string Key,
    string DefaultValue,
    string DisplayName = "",
    string Description = "",
    bool IsEncrypted = false,
    bool IsRequired = false,
    IReadOnlyList<string>? AllowedValues = null,
    string? ValidationPattern = null,
    string? ValidationMessage = null);
