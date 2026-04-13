using CommunityToolkit.Mvvm.ComponentModel;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.ViewModels;

public partial class DesignerConfirmDialogViewModel : ObservableObject
{
    [ObservableProperty] private string title = "确认";
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string confirmText = "确定";
    [ObservableProperty] private string cancelText = "取消";
    [ObservableProperty] private bool isDanger;
    [ObservableProperty] private DesignerDialogIcon icon = DesignerDialogIcon.Question;
    [ObservableProperty] private string? note;
}
