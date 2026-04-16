using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TOrbit.Plugin.Migration.ViewModels;

public sealed partial class MigrationFileDialogViewModel : ObservableObject
{
    private readonly Action<string> _saveAction;
    private string _savedContent;

    [ObservableProperty]
    private string fileName;

    [ObservableProperty]
    private string content;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private bool isDirty;

    public bool CanEnableEdit => !IsEditing;
    public bool CanExitEdit => IsEditing;
    public bool CanSave => IsEditing && IsDirty;

    public IRelayCommand EnableEditCommand { get; }
    public IRelayCommand ExitEditCommand { get; }
    public IRelayCommand SaveCommand { get; }

    public MigrationFileDialogViewModel(string fileName, string content, Action<string> saveAction)
    {
        FileName = fileName;
        Content = content;
        _savedContent = content;
        _saveAction = saveAction;

        EnableEditCommand = new RelayCommand(EnableEdit);
        ExitEditCommand = new RelayCommand(ExitEdit);
        SaveCommand = new RelayCommand(Save);
    }

    partial void OnContentChanged(string value)
    {
        IsDirty = value != _savedContent;
        RaiseCommandState();
    }

    partial void OnIsEditingChanged(bool value) => RaiseCommandState();

    private void EnableEdit()
    {
        IsEditing = true;
        RaiseCommandState();
    }

    private void ExitEdit()
    {
        Content = _savedContent;
        IsDirty = false;
        IsEditing = false;
        RaiseCommandState();
    }

    private void Save()
    {
        if (!CanSave)
            return;

        _saveAction(Content);
        _savedContent = Content;
        IsDirty = false;
        IsEditing = false;
        RaiseCommandState();
    }

    private void RaiseCommandState()
    {
        OnPropertyChanged(nameof(CanEnableEdit));
        OnPropertyChanged(nameof(CanExitEdit));
        OnPropertyChanged(nameof(CanSave));
    }
}
