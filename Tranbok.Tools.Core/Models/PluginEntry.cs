namespace Tranbok.Tools.Core.Models;

public sealed partial class PluginEntry : ObservableObject
{
    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private int sort;

    public IPlugin Plugin { get; }

    public string Id => Plugin.Descriptor.Id;
    public string Name => Plugin.Descriptor.Name;
    public string Description => Plugin.Descriptor.Description ?? string.Empty;
    public string Icon => Plugin.Descriptor.Icon ?? string.Empty;
    public string Version => Plugin.Descriptor.Version;

    public PluginEntry(IPlugin plugin, bool isEnabled = true)
    {
        Plugin = plugin;
        this.isEnabled = isEnabled;
        sort = 0;
    }
}
