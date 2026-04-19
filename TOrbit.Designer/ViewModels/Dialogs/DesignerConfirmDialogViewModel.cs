using CommunityToolkit.Mvvm.ComponentModel;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;

namespace TOrbit.Designer.ViewModels.Dialogs;

public partial class DesignerConfirmDialogViewModel : ObservableObject
{
    [ObservableProperty] private string title = LocalizationService.Current?.GetString("dialog.confirmTitle") ?? "Confirm";
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string confirmText = LocalizationService.Current?.GetString("dialog.confirm") ?? "Confirm";
    [ObservableProperty] private string cancelText = LocalizationService.Current?.GetString("dialog.cancel") ?? "Cancel";
    [ObservableProperty] private bool isDanger;
    [ObservableProperty] private DesignerDialogIcon icon = DesignerDialogIcon.Question;
    [ObservableProperty] private string? note;
}
