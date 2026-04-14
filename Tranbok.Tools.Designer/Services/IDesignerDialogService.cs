using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.ViewModels.Dialogs;

namespace Tranbok.Tools.Designer.Services;

public interface IDesignerDialogService
{
    Task<DesignerDialogResult<bool>> ShowConfirmAsync(Window owner, DesignerConfirmDialogViewModel viewModel);
    Task<DesignerDialogResult<string>> ShowPromptAsync(Window owner, DesignerPromptDialogViewModel viewModel);
    Task<DesignerDialogResult<bool>> ShowSheetAsync(Window owner, DesignerSheetViewModel viewModel);
    Task<string?> PickFileAsync(Window owner, string title, IReadOnlyList<FilePickerFileType>? fileTypes = null);
    Task<string?> PickFolderAsync(Window owner, string title);
}
