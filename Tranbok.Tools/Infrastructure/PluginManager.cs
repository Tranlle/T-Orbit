using System.Collections.ObjectModel;
using System.Windows;

namespace Tranbok.Tools.Infrastructure;

/// <summary>
/// Wraps a plugin with enable/disable toggle state and view caching.
/// </summary>
public sealed class PluginEntry : ObservableObject
{
    private bool _isEnabled = true;
    private bool _isActive;
    private int _sort;
    private FrameworkElement? _cachedView;

    public IPlugin Plugin { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    public string Id => Plugin.Id;
    public string Name => Plugin.Name;
    public string IconGlyph => Plugin.IconGlyph;
    public string Description => Plugin.Description;

    public int Sort
    {
        get => _sort;
        set => SetField(ref _sort, Math.Clamp(value, 0, 100));
    }

    public PluginEntry(IPlugin plugin, bool enabled = true)
    {
        Plugin = plugin;
        _isEnabled = enabled;
        _sort = Math.Clamp(plugin.Sort, 0, 100);
    }

    /// <summary>Returns the cached view, creating it on first access.</summary>
    public FrameworkElement GetOrCreateView() => _cachedView ??= Plugin.CreateView();
}

/// <summary>
/// Central registry for all plugins.
/// Call <see cref="Register"/> during app startup, then bind <see cref="Plugins"/> to the sidebar.
/// </summary>
public sealed class PluginManager
{
    public ObservableCollection<PluginEntry> Plugins { get; } = [];

    public void Register(IPlugin plugin, bool enabledByDefault = true)
    {
        if (Plugins.Any(p => p.Id == plugin.Id))
            throw new InvalidOperationException($"Plugin '{plugin.Id}' is already registered.");

        Plugins.Add(new PluginEntry(plugin, enabledByDefault));
    }

    public PluginEntry? Get(string id) => Plugins.FirstOrDefault(p => p.Id == id);

    public IEnumerable<PluginEntry> EnabledPlugins => Plugins.Where(p => p.IsEnabled);
}
