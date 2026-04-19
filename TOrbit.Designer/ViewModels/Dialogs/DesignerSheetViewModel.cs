using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;

namespace TOrbit.Designer.ViewModels.Dialogs;

public partial class DesignerSheetViewModel : ObservableObject
{
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string? description;
    [ObservableProperty] private Control? content;
    [ObservableProperty] private string confirmText = LocalizationService.Current?.GetString("dialog.confirm") ?? "Confirm";
    [ObservableProperty] private string cancelText = LocalizationService.Current?.GetString("dialog.cancel") ?? "Cancel";
    [ObservableProperty] private DesignerDialogIcon icon = DesignerDialogIcon.Info;
    [ObservableProperty] private string? note;
    [ObservableProperty] private double baseFontSize = 13;
    [ObservableProperty] private double dialogWidth = 880;
    [ObservableProperty] private double dialogHeight = 640;
    [ObservableProperty] private bool lockSize = true;
    [ObservableProperty] private bool hideSystemDecorations = true;
}
