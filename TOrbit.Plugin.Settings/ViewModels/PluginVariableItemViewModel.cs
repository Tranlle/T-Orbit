using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TOrbit.Plugin.Settings.ViewModels;

public sealed partial class PluginVariableItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string value;

    public string PluginId { get; }
    public string PluginName { get; }
    public string Key { get; }
    public string DefaultValue { get; }
    public string Description { get; }
    public bool IsFromMetadata { get; }
    public bool IsEncrypted { get; }

    // Encrypted variables are masked in the editor.
    public char PasswordChar => IsEncrypted ? '\u2022' : '\0';

    public IRelayCommand DeleteCommand { get; }

    public PluginVariableItemViewModel(
        string pluginId,
        string pluginName,
        string key,
        string value,
        string defaultValue = "",
        string description = "",
        bool isFromMetadata = false,
        bool isEncrypted = false,
        Action<PluginVariableItemViewModel>? onDelete = null)
    {
        PluginId = pluginId;
        PluginName = pluginName;
        Key = key;
        this.value = value;
        DefaultValue = defaultValue;
        Description = description;
        IsFromMetadata = isFromMetadata;
        IsEncrypted = isEncrypted;
        DeleteCommand = new RelayCommand(() => onDelete?.Invoke(this));
    }
}
