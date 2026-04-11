using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Tranbok.Tools.Infrastructure;

namespace Tranbok.Tools.ViewModels;

public sealed class PluginManagementViewModel : ObservableObject
{
    private PluginEntry? _selectedPlugin;

    public ObservableCollection<PluginEntry> Plugins { get; }

    public PluginEntry? SelectedPlugin
    {
        get => _selectedPlugin;
        set
        {
            if (ReferenceEquals(_selectedPlugin, value))
                return;

            if (_selectedPlugin is not null)
                _selectedPlugin.PropertyChanged -= SelectedPlugin_PropertyChanged;

            SetField(ref _selectedPlugin, value);

            if (_selectedPlugin is not null)
                _selectedPlugin.PropertyChanged += SelectedPlugin_PropertyChanged;

            OnPropertyChanged(nameof(SelectedPluginSortText));
        }
    }

    public string SelectedPluginSortText
    {
        get => (SelectedPlugin?.Sort ?? 0).ToString();
        set
        {
            if (SelectedPlugin is null)
                return;

            if (!int.TryParse(value, out var sort))
                return;

            SelectedPlugin.Sort = Math.Clamp(sort, 0, 100);
            OnPropertyChanged();
        }
    }

    public PluginManagementViewModel(PluginManager pluginManager)
    {
        Plugins = pluginManager.Plugins;
        Plugins.CollectionChanged += Plugins_CollectionChanged;
        foreach (var plugin in Plugins)
            plugin.PropertyChanged += Plugin_PropertyChanged;
        SelectedPlugin = Plugins.OrderBy(p => p.Sort).ThenBy(p => p.Name).FirstOrDefault();
    }

    private void Plugins_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (PluginEntry plugin in e.OldItems)
                plugin.PropertyChanged -= Plugin_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (PluginEntry plugin in e.NewItems)
                plugin.PropertyChanged += Plugin_PropertyChanged;
        }
    }

    private void Plugin_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginEntry.Sort))
            OnPropertyChanged(nameof(Plugins));
    }

    private void SelectedPlugin_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginEntry.Sort))
            OnPropertyChanged(nameof(SelectedPluginSortText));
    }
}
