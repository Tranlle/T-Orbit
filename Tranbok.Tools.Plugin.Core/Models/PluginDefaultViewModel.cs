namespace Tranbok.Tools.Plugin.Core.Models;

public sealed class PluginDefaultViewModel(string title, string description)
{
    public string Title { get; } = title;

    public string Description { get; } = description;
}
