using Avalonia.Controls;
using Avalonia.Interactivity;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.ViewModels;

namespace Tranbok.Tools.Designer.Controls.Dialogs;

public partial class DesignerPromptDialogWindow : Window
{
    public DesignerPromptDialogWindow()
    {
        InitializeComponent();
    }

    public DesignerPromptDialogWindow(DesignerPromptDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void ConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DesignerPromptDialogViewModel vm && string.IsNullOrWhiteSpace(vm.Value))
        {
            vm.ValidationMessage = "请输入内容后再继续。";
            return;
        }

        Close(DesignerDialogResult<string>.Confirmed((DataContext as DesignerPromptDialogViewModel)?.Value));
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(DesignerDialogResult<string>.Cancelled());
}
