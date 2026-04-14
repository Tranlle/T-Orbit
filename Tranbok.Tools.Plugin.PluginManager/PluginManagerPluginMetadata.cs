using Tranbok.Tools.Plugin.Core.Base;

namespace Tranbok.Tools.Plugin.PluginManager;

public sealed class PluginManagerPluginMetadata : PluginBaseMetadata
{
    public static PluginManagerPluginMetadata Instance { get; } = new();

    public override string Id => "plugin-manager";
    public override string Name => "插件管理";
    public override string Version => "1.0.1";
    public override string Description => "查看已加载插件，管理排序并识别默认内置插件。";
    public override string Author => "Tranbok";
    public override string Icon => "Puzzle";
    public override string Tags => "system,management,builtin";
}
