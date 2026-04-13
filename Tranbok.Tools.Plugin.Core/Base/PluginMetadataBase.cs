namespace Tranbok.Tools.Plugin.Core.Base;

public abstract class PluginBaseMetadata
{
    public abstract string Id { get; }

    public abstract string Name { get; }

    public virtual string Version => "1.0.0";

    public virtual string Description => string.Empty;

    public virtual string Author => string.Empty;

    public virtual string Icon => string.Empty;
}
