using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.ViewModels.Dialogs;

namespace Tranbok.Tools.Designer.Services;

public sealed class DesignerDialogService : IDesignerDialogService
{
    public async Task<DesignerDialogResult<bool>> ShowConfirmAsync(Window owner, DesignerConfirmDialogViewModel viewModel)
    {
        var dialog = CreateDialogWindow(owner, viewModel.Title, 520, 280);
        var body = new StackPanel { Spacing = 16 };
        body.Children.Add(new TextBlock
        {
            Text = viewModel.Message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 15
        });

        if (!string.IsNullOrWhiteSpace(viewModel.Note))
        {
            body.Children.Add(new TextBlock
            {
                Text = viewModel.Note,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.72
            });
        }

        dialog.Content = WrapDialogContent(body, viewModel.ConfirmText, viewModel.CancelText,
            () => dialog.Close(DesignerDialogResult<bool>.Confirmed(true)),
            () => dialog.Close(DesignerDialogResult<bool>.Cancelled(false)));

        var result = await dialog.ShowDialog<DesignerDialogResult<bool>>(owner);
        return result ?? DesignerDialogResult<bool>.Cancelled(false);
    }

    public async Task<DesignerDialogResult<string>> ShowPromptAsync(Window owner, DesignerPromptDialogViewModel viewModel)
    {
        var dialog = CreateDialogWindow(owner, viewModel.Title, 560, 320);
        var input = new TextBox
        {
            PlaceholderText = viewModel.Placeholder,
            Text = viewModel.Value,
            MinWidth = 420
        };

        var body = new StackPanel { Spacing = 16 };
        body.Children.Add(new TextBlock
        {
            Text = viewModel.Message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 15
        });
        body.Children.Add(input);

        if (!string.IsNullOrWhiteSpace(viewModel.Note))
        {
            body.Children.Add(new TextBlock
            {
                Text = viewModel.Note,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.72
            });
        }

        dialog.Content = WrapDialogContent(body, viewModel.ConfirmText, viewModel.CancelText,
            () => dialog.Close(DesignerDialogResult<string>.Confirmed(input.Text?.Trim() ?? string.Empty)),
            () => dialog.Close(DesignerDialogResult<string>.Cancelled()));

        var result = await dialog.ShowDialog<DesignerDialogResult<string>>(owner);
        return result ?? DesignerDialogResult<string>.Cancelled();
    }

    public async Task<DesignerDialogResult<bool>> ShowSheetAsync(Window owner, DesignerSheetViewModel viewModel)
    {
        var scale = Math.Max(0.85, viewModel.BaseFontSize / 14d);
        var width = viewModel.DialogWidth * scale;
        var height = viewModel.DialogHeight * scale;
        var dialog = CreateDialogWindow(owner, viewModel.Title, width, height, !viewModel.LockSize, viewModel.HideSystemDecorations);
        var body = new StackPanel { Spacing = 14 * scale };

        if (viewModel.Content is not null)
        {
            ApplySheetResources(viewModel.Content, viewModel.BaseFontSize);
            body.Children.Add(viewModel.Content);
        }

        dialog.Content = WrapSheetDialogContent(body, viewModel, scale,
            () => dialog.Close(DesignerDialogResult<bool>.Confirmed(true)),
            () => dialog.Close(DesignerDialogResult<bool>.Cancelled(false)));

        var result = await dialog.ShowDialog<DesignerDialogResult<bool>>(owner);
        return result ?? DesignerDialogResult<bool>.Cancelled(false);
    }

    public async Task<string?> PickFileAsync(Window owner, string title, IReadOnlyList<FilePickerFileType>? fileTypes = null)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypes?.ToList()
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> PickFolderAsync(Window owner, string title)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    private static Window CreateDialogWindow(Window owner, string title, double width, double height)
    {
        return CreateDialogWindow(owner, title, width, height, true, false);
    }

    private static Window CreateDialogWindow(Window owner, string title, double width, double height, bool canResize, bool hideSystemDecorations)
    {
        return new Window
        {
            Title = title,
            Width = width,
            Height = height,
            MinWidth = canResize ? Math.Min(width, 480) : width,
            MaxWidth = canResize ? double.PositiveInfinity : width,
            MinHeight = canResize ? Math.Min(height, 260) : height,
            MaxHeight = canResize ? double.PositiveInfinity : height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = canResize,
            ShowInTaskbar = false,
            WindowDecorations = hideSystemDecorations ? WindowDecorations.None : WindowDecorations.Full,
            Background = owner.Background
        };
    }

    private static Control WrapSheetDialogContent(Control body, DesignerSheetViewModel viewModel, double scale, Action onConfirm, Action onCancel)
    {
        var titleBlock = new TextBlock
        {
            Text = viewModel.Title,
            FontSize = viewModel.BaseFontSize * 1.65,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };

        var header = new StackPanel
        {
            Spacing = 6 * scale
        };
        header.Children.Add(titleBlock);

        if (!string.IsNullOrWhiteSpace(viewModel.Description))
        {
            header.Children.Add(new TextBlock
            {
                Text = viewModel.Description,
                FontSize = viewModel.BaseFontSize,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.8
            });
        }

        var confirmButton = new Button
        {
            Content = viewModel.ConfirmText,
            MinWidth = 82 * scale,
            Height = 34 * scale,
            Padding = new Thickness(14 * scale, 0)
        };
        confirmButton.Click += (_, _) => onConfirm();

        var cancelButton = new Button
        {
            Content = string.IsNullOrWhiteSpace(viewModel.CancelText) ? "关闭" : viewModel.CancelText,
            MinWidth = 82 * scale,
            Height = 34 * scale,
            Padding = new Thickness(14 * scale, 0)
        };
        cancelButton.Click += (_, _) => onCancel();

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8 * scale,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        footer.Children.Add(cancelButton);
        footer.Children.Add(confirmButton);

        var contentStack = new StackPanel
        {
            Spacing = 10 * scale
        };
        contentStack.Children.Add(header);
        contentStack.Children.Add(body);

        if (!string.IsNullOrWhiteSpace(viewModel.Note))
        {
            contentStack.Children.Add(new TextBlock
            {
                Text = viewModel.Note,
                FontSize = viewModel.BaseFontSize * 0.92,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.72
            });
        }

        var grid = new Grid
        {
            Margin = new Thickness(18 * scale),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12 * scale
        };

        grid.Children.Add(contentStack);
        Grid.SetRow(footer, 1);
        grid.Children.Add(footer);

        var surfaceBrush = Application.Current?.Resources["TranbokSurfaceBrush"] as IBrush;
        var borderBrush = Application.Current?.Resources["TranbokBorderBrush"] as IBrush;

        return new Border
        {
            Background = surfaceBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20 * scale),
            Padding = new Thickness(4),
            Child = grid
        };
    }

    private static void ApplySheetResources(Control content, double baseFontSize)
    {
        content.Resources["SheetBaseFontSize"] = baseFontSize;
        content.Resources["SheetSectionTitleFontSize"] = baseFontSize * 1.23;
        content.Resources["SheetLabelFontSize"] = baseFontSize;
        content.Resources["SheetCaptionFontSize"] = baseFontSize * 0.92;
        content.Resources["SheetControlHeight"] = Math.Round(baseFontSize * 2.85);
    }

    private static Control WrapDialogContent(Control body, string confirmText, string cancelText, Action onConfirm, Action onCancel)
    {
        var confirmButton = new Button
        {
            Content = confirmText,
            MinWidth = 96
        };
        confirmButton.Click += (_, _) => onConfirm();

        var cancelButton = new Button
        {
            Content = string.IsNullOrWhiteSpace(cancelText) ? "关闭" : cancelText,
            MinWidth = 96
        };
        cancelButton.Click += (_, _) => onCancel();

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        footer.Children.Add(cancelButton);
        footer.Children.Add(confirmButton);

        var grid = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 20
        };

        grid.Children.Add(body);
        Grid.SetRow(footer, 1);
        grid.Children.Add(footer);
        return grid;
    }
}
