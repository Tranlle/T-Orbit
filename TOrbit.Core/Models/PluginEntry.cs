using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Core.Models;

public sealed partial class PluginEntry : ObservableObject
{
    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private int sort;

    [ObservableProperty]
    private Exception? lastError;

    [ObservableProperty]
    private DateTimeOffset stateChangedAt = DateTimeOffset.Now;

    public IPlugin Plugin { get; }

    public bool IsBuiltIn { get; }
    public bool CanDisable { get; }
    public string BuiltInHint { get; }

    public string Id => Plugin.Descriptor.Id;
    public string Name => Plugin.Descriptor.Name;
    public string Description => Plugin.Descriptor.Description ?? string.Empty;
    public string Icon => Plugin.Descriptor.Icon ?? string.Empty;
    public string Version => Plugin.Descriptor.Version;
    public string Tags => Plugin.Descriptor.Tags ?? string.Empty;
    public IReadOnlyList<PluginCapability> Capabilities => Plugin.Descriptor.Capabilities ?? [];
    public PluginState State => Plugin is BasePlugin basePlugin ? basePlugin.State : PluginState.Unknown;
    public PluginKind Kind => Plugin.Descriptor.Kind;
    public IReadOnlyList<string> DisplayTags => Tags
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(tag => !string.IsNullOrWhiteSpace(tag))
        .Take(2)
        .ToArray();
    public string CapabilitiesSummary => Capabilities.Count == 0
        ? "none"
        : string.Join(", ", Capabilities.Select(capability => capability.ToString()));

    public PluginEntry(
        IPlugin plugin,
        bool isEnabled = true,
        bool isBuiltIn = false,
        bool canDisable = true,
        string? builtInHint = null)
    {
        Plugin = plugin;
        this.isEnabled = isEnabled;
        IsBuiltIn = isBuiltIn;
        CanDisable = canDisable;
        BuiltInHint = builtInHint ?? string.Empty;
        sort = 0;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (CanDisable || value)
            return;

        isEnabled = true;
        OnPropertyChanged(nameof(IsEnabled));
    }

    public void NotifyStateChanged()
    {
        StateChangedAt = DateTimeOffset.Now;
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(LastError));
    }
}
