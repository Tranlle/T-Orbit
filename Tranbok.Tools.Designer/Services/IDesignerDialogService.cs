using System.Threading.Tasks;
using Avalonia.Controls;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.ViewModels;

namespace Tranbok.Tools.Designer.Services;

public interface IDesignerDialogService
{
    Task<DesignerDialogResult<bool>> ShowConfirmAsync(Window owner, DesignerConfirmDialogViewModel viewModel);
    Task<DesignerDialogResult<string>> ShowPromptAsync(Window owner, DesignerPromptDialogViewModel viewModel);
    Task<DesignerDialogResult<bool>> ShowSheetAsync(Window owner, DesignerSheetViewModel viewModel);
}
