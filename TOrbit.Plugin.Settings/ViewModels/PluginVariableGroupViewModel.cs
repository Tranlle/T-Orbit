using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TOrbit.Plugin.Settings.ViewModels;

public sealed partial class PluginVariableGroupViewModel : ObservableObject
{
    public string PluginId { get; init; } = string.Empty;
    public string PluginName { get; init; } = string.Empty;
    public IReadOnlyList<string> CapabilityTags { get; init; } = [];
    public string CapabilitySummary => CapabilityTags.Count == 0 ? "未声明能力" : string.Join(" / ", CapabilityTags);
    public ObservableCollection<PluginVariableItemViewModel> Variables { get; } = [];
    public int Count => Variables.Count;

    public PluginVariableGroupViewModel()
    {
        Variables.CollectionChanged += OnVariablesChanged;
    }

    private void OnVariablesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Count));
    }
}
