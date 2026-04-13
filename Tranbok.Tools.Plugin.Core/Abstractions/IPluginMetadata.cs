namespace Tranbok.Tools.Plugin.Core.Abstractions;

public interface IPluginMetadata
{
    string Id { get; }

    string Name { get; }

    string Version { get; }

    string Description { get; }

    string Author { get; }

    string Icon { get; }
}
