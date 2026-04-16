using System.Text.RegularExpressions;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Core.Base;

public abstract class PluginBaseMetadata
{
    /// <summary>
    /// 插件唯一标识，必须遵循反向域名命名约定：
    /// 至少两段、全小写字母/数字/连字符，段间以 . 分隔，每段不能以连字符开头或结尾。
    /// 示例：torbit.settings、com.example.my-plugin
    /// </summary>
    public abstract string Id { get; }

    private static readonly Regex IdPattern =
        new(@"^[a-z0-9][a-z0-9\-]*(\.[a-z0-9][a-z0-9\-]*)+$", RegexOptions.Compiled);

    /// <summary>
    /// 校验 Id 格式是否符合反向域名约定，不符合时抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    public void ValidateId()
    {
        if (!IdPattern.IsMatch(Id))
            throw new InvalidOperationException(
                $"Plugin Id \"{Id}\" does not follow the reverse-domain naming convention. " +
                $"Expected format: lowercase segments separated by dots, e.g. \"tranbok.my-plugin\" or \"com.example.tool\".");
    }

    public abstract string Name { get; }

    public virtual string Version => "1.0.0";

    public virtual string Description => string.Empty;

    public virtual string Author => string.Empty;

    public virtual string Icon => string.Empty;

    public virtual string Tags => string.Empty;

    /// <summary>
    /// 插件声明的环境变量定义列表。每个定义包含键名、默认值及展示信息。
    /// 基座与设置插件不负责校验，具体的错误处理由各插件自行实现。
    /// </summary>
    public virtual IReadOnlyList<PluginVariableDefinition> VariableDefinitions => [];

    public virtual PluginKind Kind => PluginKind.Visual;

    public virtual IReadOnlyList<PluginCapability> Capabilities => [];
}
