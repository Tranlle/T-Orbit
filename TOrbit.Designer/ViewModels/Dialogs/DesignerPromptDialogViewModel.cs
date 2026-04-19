using CommunityToolkit.Mvvm.ComponentModel;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;

namespace TOrbit.Designer.ViewModels.Dialogs;

public partial class DesignerPromptDialogViewModel : ObservableObject
{
    [ObservableProperty] private string title = LocalizationService.Current?.GetString("dialog.promptTitle") ?? "Input";
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string value = string.Empty;
    [ObservableProperty] private string placeholder = string.Empty;
    [ObservableProperty] private string confirmText = LocalizationService.Current?.GetString("dialog.confirm") ?? "Confirm";
    [ObservableProperty] private string cancelText = LocalizationService.Current?.GetString("dialog.cancel") ?? "Cancel";
    [ObservableProperty] private string? validationMessage;
    [ObservableProperty] private DesignerDialogIcon icon = DesignerDialogIcon.Info;
    [ObservableProperty] private string? note;
}
