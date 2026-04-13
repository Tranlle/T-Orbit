using CommunityToolkit.Mvvm.ComponentModel;
using Tranbok.Tools.Designer.Models;

namespace Tranbok.Tools.Designer.ViewModels;

public partial class DesignerPromptDialogViewModel : ObservableObject
{
    [ObservableProperty] private string title = "请输入";
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string value = string.Empty;
    [ObservableProperty] private string placeholder = string.Empty;
    [ObservableProperty] private string confirmText = "确定";
    [ObservableProperty] private string cancelText = "取消";
    [ObservableProperty] private string? validationMessage;
    [ObservableProperty] private DesignerDialogIcon icon = DesignerDialogIcon.Info;
    [ObservableProperty] private string? note;
}
