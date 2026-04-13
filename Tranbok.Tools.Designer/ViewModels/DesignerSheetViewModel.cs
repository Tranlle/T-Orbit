using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.ViewModels;

public partial class DesignerSheetViewModel : ObservableObject
{
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string? description;
    [ObservableProperty] private Control? content;
    [ObservableProperty] private string confirmText = "确定";
    [ObservableProperty] private string cancelText = "取消";
    [ObservableProperty] private DesignerDialogIcon icon = DesignerDialogIcon.Info;
    [ObservableProperty] private string? note;
}
