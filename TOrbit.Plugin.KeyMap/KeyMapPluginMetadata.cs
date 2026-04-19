using TOrbit.Plugin.Core.Base;

namespace TOrbit.Plugin.KeyMap;

public sealed class KeyMapPluginMetadata : PluginBaseMetadata
{
    public static KeyMapPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.keymap";
    public override string Name => "快捷键";
    public override string Version => "1.0.0";
    public override string Description => "快捷键管理";
    public override string Author => "T-Orbit";
    public override string Icon => "Keyboard";
    public override string Tags => "System,Keyboard,Builtin";
}
