using Avalonia.Controls;
using Tranbok.Tools.Designer.Controls.Dialogs;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.ViewModels;

namespace Tranbok.Tools.Designer.Services;

public sealed class DesignerDialogService : IDesignerDialogService
{
    public async Task<DesignerDialogResult<bool>> ShowConfirmAsync(Window owner, DesignerConfirmDialogViewModel viewModel)
    {
        var dialog = new DesignerConfirmDialogWindow(viewModel);
        var result = await dialog.ShowDialog<DesignerDialogResult<bool>>(owner);
        return result ?? DesignerDialogResult<bool>.Cancelled(false);
    }

    public async Task<DesignerDialogResult<string>> ShowPromptAsync(Window owner, DesignerPromptDialogViewModel viewModel)
    {
        var dialog = new DesignerPromptDialogWindow(viewModel);
        var result = await dialog.ShowDialog<DesignerDialogResult<string>>(owner);
        return result ?? DesignerDialogResult<string>.Cancelled();
    }

    public async Task<DesignerDialogResult<bool>> ShowSheetAsync(Window owner, DesignerSheetViewModel viewModel)
    {
        var dialog = new DesignerSheetWindow(viewModel);
        var result = await dialog.ShowDialog<DesignerDialogResult<bool>>(owner);
        return result ?? DesignerDialogResult<bool>.Cancelled(false);
    }
}
