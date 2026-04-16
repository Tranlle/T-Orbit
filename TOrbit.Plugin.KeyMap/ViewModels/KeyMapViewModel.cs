using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TOrbit.Core.Services;

namespace TOrbit.Plugin.KeyMap.ViewModels;

public sealed partial class KeyMapViewModel : ObservableObject, IDisposable
{
    private readonly IKeyMapService _keyMapService;
    private List<KeyMapBindingViewModel> _allBindings = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private KeyMapBindingViewModel? selectedBinding;

    public ObservableCollection<KeyMapGroupViewModel> Groups { get; } = [];

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetAllCommand { get; }
    public IRelayCommand<KeyMapBindingViewModel> SelectBindingCommand { get; }

    public KeyMapViewModel(IKeyMapService keyMapService)
    {
        _keyMapService = keyMapService;
        _keyMapService.Changed += KeyMapServiceChanged;

        SaveCommand = new RelayCommand(DoSave);
        ResetAllCommand = new RelayCommand(DoResetAll);
        SelectBindingCommand = new RelayCommand<KeyMapBindingViewModel>(b => SelectedBinding = b);

        RebuildBindings();
    }

    private void KeyMapServiceChanged(object? sender, EventArgs e)
    {
        RebuildBindings();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedBindingChanged(KeyMapBindingViewModel? oldValue, KeyMapBindingViewModel? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }

    private void RebuildBindings()
    {
        _allBindings = _keyMapService.Entries
            .Select(e => new KeyMapBindingViewModel(e, _keyMapService))
            .ToList();

        ApplyFilter();

        SelectedBinding = _allBindings.FirstOrDefault();
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allBindings
            : _allBindings.Where(b =>
                b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                b.CurrentKeyDisplay.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                b.PluginName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Groups.Clear();

        foreach (var group in filtered
            .GroupBy(b => b.PluginName)
            .OrderBy(g => g.Key))
        {
            Groups.Add(new KeyMapGroupViewModel(group.Key, group));
        }
    }

    private void DoSave()
    {
        _keyMapService.Save();
    }

    private void DoResetAll()
    {
        _keyMapService.Reset();

        foreach (var binding in _allBindings)
        {
            binding.CurrentKeyDisplay = binding.DefaultKey;
            binding.IsEnabled = true;
        }
    }

    public void Dispose()
    {
        _keyMapService.Changed -= KeyMapServiceChanged;
    }
}
