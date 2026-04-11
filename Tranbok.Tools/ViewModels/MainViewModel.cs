using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Tranbok.Tools.Infrastructure;
using Tranbok.Tools.Views;

namespace Tranbok.Tools.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    public sealed class NavigationItem : ObservableObject
    {
        private bool _isActive;

        public string Id { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string IconGlyph { get; init; } = string.Empty;
        public int Sort { get; init; }
        public bool IsPinned { get; init; }
        public PluginEntry? Plugin { get; init; }

        public bool IsActive
        {
            get => _isActive;
            set => SetField(ref _isActive, value);
        }
    }

    private readonly FrameworkElement _pluginManagerView;
    private readonly FrameworkElement _settingsView;
    private NavigationItem? _activeNavigation;
    private FrameworkElement? _activeView;

    public bool HasActiveView => ActiveView is not null;

    public PluginManager PluginManager { get; }
    public ObservableCollection<NavigationItem> NavigationItems { get; } = [];

    public NavigationItem? ActiveNavigation
    {
        get => _activeNavigation;
        set
        {
            if (ReferenceEquals(_activeNavigation, value))
                return;

            SetField(ref _activeNavigation, value);

            foreach (var item in NavigationItems)
                item.IsActive = ReferenceEquals(item, value);

            ActiveView = ResolveView(value);
        }
    }

    public FrameworkElement? ActiveView
    {
        get => _activeView;
        private set
        {
            if (ReferenceEquals(_activeView, value))
                return;

            SetField(ref _activeView, value);
            OnPropertyChanged(nameof(HasActiveView));
        }
    }

    public RelayCommand<NavigationItem> ActivateNavigationCommand { get; }

    public MainViewModel(PluginManager pluginManager)
    {
        PluginManager = pluginManager;

        _pluginManagerView = new PluginManagementView
        {
            DataContext = new PluginManagementViewModel(pluginManager)
        };
        _settingsView = new SettingsView
        {
            DataContext = new SettingsViewModel()
        };

        BuildNavigationItems();

        pluginManager.Plugins.CollectionChanged += Plugins_CollectionChanged;
        foreach (var plugin in pluginManager.Plugins)
            plugin.PropertyChanged += Plugin_PropertyChanged;

        ActivateNavigationCommand = new RelayCommand<NavigationItem>(item =>
        {
            if (item is not null)
                ActiveNavigation = item;
        });

        ActiveNavigation = NavigationItems.FirstOrDefault();
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

        BuildNavigationItems();
    }

    private void Plugin_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginEntry.IsEnabled))
            BuildNavigationItems();
    }

    private void BuildNavigationItems()
    {
        var selectedId = ActiveNavigation?.Id;

        NavigationItems.Clear();

        foreach (var plugin in PluginManager.EnabledPlugins.OrderBy(p => p.Sort).ThenBy(p => p.Name))
        {
            NavigationItems.Add(new NavigationItem
            {
                Id = $"plugin:{plugin.Id}",
                Title = plugin.Name,
                IconGlyph = plugin.IconGlyph,
                Sort = plugin.Sort,
                Plugin = plugin
            });
        }

        NavigationItems.Add(new NavigationItem { Id = "plugins", Title = "插件管理", Sort = 99, IsPinned = true });
        NavigationItems.Add(new NavigationItem { Id = "settings", Title = "设置", Sort = 100, IsPinned = true });

        var next = NavigationItems.FirstOrDefault(x => x.Id == selectedId)
                   ?? NavigationItems.FirstOrDefault();

        _activeNavigation = null;
        ActiveNavigation = next;
    }

    private FrameworkElement? ResolveView(NavigationItem? item)
    {
        if (item is null)
            return null;

        return item.Id switch
        {
            "plugins" => _pluginManagerView,
            "settings" => _settingsView,
            _ => item.Plugin?.GetOrCreateView()
        };
    }

    private static FrameworkElement BuildPlaceholder(string text)
    {
        return new Grid
        {
            Children =
            {
                new TextBlock
                {
                    Text = text,
                    FontSize = 16,
                    Opacity = 0.6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }
}
