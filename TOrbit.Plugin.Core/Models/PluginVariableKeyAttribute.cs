namespace TOrbit.Plugin.Core.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PluginVariableKeyAttribute : Attribute
{
    public PluginVariableKeyAttribute(string key)
    {
        Key = key;
    }

    public string Key { get; }
}
