using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.ViewModels.Dialogs;

public partial class DesignerSheetViewModel : ObservableObject
{
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string? description;
    [ObservableProperty] private Control? content;
    [ObservableProperty] private string confirmText = "确定";
    [ObservableProperty] private string cancelText = "取消";
    [ObservableProperty] private DesignerDialogIcon icon = DesignerDialogIcon.Info;
    [ObservableProperty] private string? note;
    [ObservableProperty] private double baseFontSize = 13;
    [ObservableProperty] private double dialogWidth = 880;
    [ObservableProperty] private double dialogHeight = 640;
    [ObservableProperty] private bool lockSize = true;
    [ObservableProperty] private bool hideSystemDecorations = true;
}
