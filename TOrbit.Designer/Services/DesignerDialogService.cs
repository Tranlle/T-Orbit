using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Material.Icons;
using Material.Icons.Avalonia;
using TOrbit.Designer.Models;
using TOrbit.Designer.ViewModels.Dialogs;

namespace TOrbit.Designer.Services;

public sealed class DesignerDialogService : IDesignerDialogService
{
    public async Task<DesignerDialogResult<bool>> ShowConfirmAsync(Window owner, DesignerConfirmDialogViewModel viewModel)
    {
        var dialog = CreateDialogWindow(owner, 520, 0, false);

        var body = new StackPanel { Spacing = 12 };
        body.Children.Add(new TextBlock
        {
            Text = viewModel.Message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 15,
            LineHeight = 24
        });

        if (!string.IsNullOrWhiteSpace(viewModel.Note))
        {
            body.Children.Add(new Border
            {
                Background = GetResource<IBrush>(owner, "TOrbitBadgeWarningBackgroundBrush"),
                BorderBrush = GetResource<IBrush>(owner, "TOrbitBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8),
                Child = new TextBlock
                {
                    Text = viewModel.Note,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                    Foreground = GetResource<IBrush>(owner, "TOrbitBadgeWarningForegroundBrush")
                }
            });
        }

        dialog.Content = BuildDialogShell(
            dialog, owner,
            viewModel.Title, null,
            body,
            viewModel.ConfirmText, viewModel.CancelText,
            isConfirmAccent: true,
            onConfirm: () => dialog.Close(DesignerDialogResult<bool>.Confirmed(true)),
            onCancel: () => dialog.Close(DesignerDialogResult<bool>.Cancelled(false)));

        var result = await dialog.ShowDialog<DesignerDialogResult<bool>>(owner);
        return result ?? DesignerDialogResult<bool>.Cancelled(false);
    }

    public async Task<DesignerDialogResult<string>> ShowPromptAsync(Window owner, DesignerPromptDialogViewModel viewModel)
    {
        var dialog = CreateDialogWindow(owner, 560, 0, false);

        var input = new TextBox
        {
            PlaceholderText = viewModel.Placeholder,
            Text = viewModel.Value,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var body = new StackPanel { Spacing = 12 };
        body.Children.Add(new TextBlock
        {
            Text = viewModel.Message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 15,
            LineHeight = 24
        });
        body.Children.Add(input);

        if (!string.IsNullOrWhiteSpace(viewModel.Note))
        {
            body.Children.Add(new Border
            {
                Background = GetResource<IBrush>(owner, "TOrbitBadgeWarningBackgroundBrush"),
                BorderBrush = GetResource<IBrush>(owner, "TOrbitBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8),
                Child = new TextBlock
                {
                    Text = viewModel.Note,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                    Foreground = GetResource<IBrush>(owner, "TOrbitBadgeWarningForegroundBrush")
                }
            });
        }

        dialog.Content = BuildDialogShell(
            dialog, owner,
            viewModel.Title, null,
            body,
            viewModel.ConfirmText, viewModel.CancelText,
            isConfirmAccent: true,
            onConfirm: () => dialog.Close(DesignerDialogResult<string>.Confirmed(input.Text?.Trim() ?? string.Empty)),
            onCancel: () => dialog.Close(DesignerDialogResult<string>.Cancelled()));

        var result = await dialog.ShowDialog<DesignerDialogResult<string>>(owner);
        return result ?? DesignerDialogResult<string>.Cancelled();
    }

    public async Task<DesignerDialogResult<bool>> ShowSheetAsync(Window owner, DesignerSheetViewModel viewModel)
    {
        var scale = Math.Max(0.85, viewModel.BaseFontSize / 14d);
        var dialog = CreateDialogWindow(owner, viewModel.DialogWidth * scale, viewModel.DialogHeight * scale, !viewModel.LockSize);

        var body = new StackPanel { Spacing = Math.Round(14 * scale) };

        if (viewModel.Content is not null)
        {
            ApplySheetResources(viewModel.Content, viewModel.BaseFontSize);
            body.Children.Add(viewModel.Content);
        }

        dialog.Content = BuildDialogShell(
            dialog, owner,
            viewModel.Title, viewModel.Description,
            body,
            viewModel.ConfirmText,
            viewModel.CancelText ?? string.Empty,
            isConfirmAccent: true,
            onConfirm: () => dialog.Close(DesignerDialogResult<bool>.Confirmed(true)),
            onCancel: () => dialog.Close(DesignerDialogResult<bool>.Cancelled(false)),
            noteText: viewModel.Note,
            scale: scale);

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

    // ── Window factory ────────────────────────────────────────────────────

    private static Window CreateDialogWindow(Window owner, double width, double height, bool canResize)
    {
        var w = new Window
        {
            Width = width > 0 ? width : 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = canResize,
            ShowInTaskbar = false,
            ExtendClientAreaToDecorationsHint = true,
            ExtendClientAreaTitleBarHeightHint = -1,
            Background = owner.Background,
            SizeToContent = height <= 0 ? SizeToContent.Height : SizeToContent.Manual
        };

        if (height > 0)
        {
            w.Height = height;
            if (canResize)
            {
                w.MinWidth = Math.Min(width, 480);
                w.MinHeight = Math.Min(height, 260);
            }
            else
            {
                w.MinWidth = width;
                w.MaxWidth = width;
                w.MinHeight = height;
                w.MaxHeight = height;
            }
        }
        else
        {
            w.MaxWidth = width;
        }

        return w;
    }

    // ── Unified dialog shell builder ──────────────────────────────────────

    private static Control BuildDialogShell(
        Window dialog,
        Window owner,
        string title,
        string? description,
        Control body,
        string confirmText,
        string cancelText,
        bool isConfirmAccent,
        Action onConfirm,
        Action onCancel,
        string? noteText = null,
        double scale = 1.0)
    {
        var accentBrush = GetResource<IBrush>(owner, "TOrbitAccentBrush");
        var surfaceBrush = GetResource<IBrush>(owner, "TOrbitSurfaceBrush");
        var surfaceElevatedBrush = GetResource<IBrush>(owner, "TOrbitSurfaceElevatedBrush");
        var borderBrush = GetResource<IBrush>(owner, "TOrbitBorderBrush");
        var textPrimaryBrush = GetResource<IBrush>(owner, "TOrbitTextPrimaryBrush");
        var textSecondaryBrush = GetResource<IBrush>(owner, "TOrbitTextSecondaryBrush");
        var textMutedBrush = GetResource<IBrush>(owner, "TOrbitTextMutedBrush");
        var accentFgBrush = GetResource<IBrush>(owner, "TOrbitAccentForegroundBrush");
        var dangerBgBrush = GetResource<IBrush>(owner, "TOrbitBadgeDangerBackgroundBrush");
        var dangerFgBrush = GetResource<IBrush>(owner, "TOrbitBadgeDangerForegroundBrush");

        // ── Title bar ─────────────────────────────────────────────────
        // ── Title bar left content: title alone, or title+description stacked ──
        Control titleContent;
        if (!string.IsNullOrWhiteSpace(description))
        {
            var headerStack = new StackPanel { Spacing = 4 };
            headerStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = Math.Round(16 * scale),
                FontWeight = FontWeight.SemiBold,
                Foreground = textPrimaryBrush
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = Math.Round(13 * scale),
                Foreground = textMutedBrush,
                TextWrapping = TextWrapping.Wrap
            });
            titleContent = headerStack;
        }
        else
        {
            titleContent = new TextBlock
            {
                Text = title,
                FontSize = Math.Round(16 * scale),
                FontWeight = FontWeight.SemiBold,
                Foreground = textPrimaryBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        var closeBtn = new Button
        {
            Width = 30,
            Height = 30,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = new MaterialIcon { Kind = MaterialIconKind.Close, Width = 14, Height = 14, Foreground = textSecondaryBrush },
            CornerRadius = new CornerRadius(6)
        };
        closeBtn.Click += (_, _) => onCancel();

        var titleBar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 0, 0, Math.Round(12 * scale))
        };
        titleBar.Children.Add(titleContent);
        Grid.SetColumn(closeBtn, 1);
        titleBar.Children.Add(closeBtn);

        // Make title bar draggable
        titleBar.PointerPressed += (sender, e) =>
        {
            if (!e.GetCurrentPoint(dialog).Properties.IsLeftButtonPressed) return;
            var src = e.Source as Control;
            while (src is not null && !ReferenceEquals(src, sender))
            {
                if (src is Button) return;
                src = src.Parent as Control;
            }
            dialog.BeginMoveDrag(e);
        };

        // ── Accent top stripe ─────────────────────────────────────────
        var accentStripe = new Border
        {
            Height = 3,
            Background = accentBrush,
            CornerRadius = new CornerRadius(15, 15, 0, 0)
        };

        // ── Separator ─────────────────────────────────────────────────
        var separator = new Border
        {
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(0, 0, 0, Math.Round(16 * scale))
        };

        // ── Note ──────────────────────────────────────────────────────
        Border? noteBorder = null;
        if (!string.IsNullOrWhiteSpace(noteText))
        {
            noteBorder = new Border
            {
                Background = GetResource<IBrush>(owner, "TOrbitBadgeWarningBackgroundBrush"),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(0, Math.Round(12 * scale), 0, 0),
                Child = new TextBlock
                {
                    Text = noteText,
                    FontSize = Math.Round(12 * scale),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = GetResource<IBrush>(owner, "TOrbitBadgeWarningForegroundBrush")
                }
            };
        }

        // ── Buttons ───────────────────────────────────────────────────
        // Only render a button when its text is non-empty.
        // If both are empty fall back to a single "关闭" so the dialog is always dismissible.
        bool showCancel  = !string.IsNullOrWhiteSpace(cancelText);
        bool showConfirm = !string.IsNullOrWhiteSpace(confirmText);
        if (!showCancel && !showConfirm)
        {
            showCancel = true;
            cancelText = LocalizationService.Current?.GetString("dialog.close") ?? "Close";
        }

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Math.Round(8 * scale),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        if (showCancel)
        {
            var cancelBtn = new Button
            {
                Content = cancelText,
                MinWidth = Math.Round(88 * scale),
                Height = Math.Round(36 * scale),
                Padding = new Thickness(Math.Round(16 * scale), 0),
                Background = surfaceElevatedBrush,
                Foreground = textPrimaryBrush,
                BorderBrush = borderBrush
            };
            cancelBtn.Click += (_, _) => onCancel();
            footer.Children.Add(cancelBtn);
        }

        if (showConfirm)
        {
            var confirmBtn = new Button
            {
                Content = confirmText,
                MinWidth = Math.Round(88 * scale),
                Height = Math.Round(36 * scale),
                Padding = new Thickness(Math.Round(16 * scale), 0),
                Background = isConfirmAccent ? accentBrush : surfaceElevatedBrush,
                Foreground = isConfirmAccent ? accentFgBrush : textPrimaryBrush,
                BorderBrush = isConfirmAccent ? accentBrush : borderBrush
            };
            confirmBtn.Click += (_, _) => onConfirm();
            footer.Children.Add(confirmBtn);
        }

        var footerSeparator = new Border
        {
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(0, Math.Round(16 * scale), 0, Math.Round(16 * scale))
        };

        // ── Content assembly ──────────────────────────────────────────
        // Layout: fixed header rows (Auto) + scrollable body (*) + fixed footer (Auto)
        // This guarantees the footer buttons are always visible regardless of body size.
        var scrollBody = new StackPanel { Spacing = 0 };
        scrollBody.Children.Add(body);
        if (noteBorder is not null)
            scrollBody.Children.Add(noteBorder);

        var bodyScroll = new ScrollViewer
        {
            Content = scrollBody,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var footerStack = new StackPanel { Spacing = 0 };
        footerStack.Children.Add(footerSeparator);
        footerStack.Children.Add(footer);

        var innerGrid = new Grid
        {
            Margin = new Thickness(Math.Round(20 * scale)),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            RowSpacing = 0
        };
        // Row 0: title bar
        innerGrid.Children.Add(titleBar);
        // Row 1: separator
        Grid.SetRow(separator, 1);
        innerGrid.Children.Add(separator);
        // Row 2: scrollable body (fills remaining space)
        Grid.SetRow(bodyScroll, 2);
        innerGrid.Children.Add(bodyScroll);
        // Row 3: footer (always anchored to bottom)
        Grid.SetRow(footerStack, 3);
        innerGrid.Children.Add(footerStack);

        // ── Outer shell ───────────────────────────────────────────────
        var shell = new Border
        {
            Background = surfaceBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(Math.Round(18 * scale)),
            BoxShadow = BoxShadows.Parse("0 8 32 0 #30000000"),
            Padding = new Thickness(0),
            ClipToBounds = false
        };

        var shellContent = new StackPanel { Spacing = 0 };
        shellContent.Children.Add(accentStripe);
        shellContent.Children.Add(innerGrid);
        shell.Child = shellContent;

        return shell;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void ApplySheetResources(Control content, double baseFontSize)
    {
        content.Resources["SheetBaseFontSize"] = baseFontSize;
        content.Resources["SheetSectionTitleFontSize"] = baseFontSize * 1.23;
        content.Resources["SheetLabelFontSize"] = baseFontSize;
        content.Resources["SheetCaptionFontSize"] = baseFontSize * 0.92;
        content.Resources["SheetControlHeight"] = Math.Round(baseFontSize * 2.85);
    }

    private static T? GetResource<T>(Window owner, string key) where T : class
    {
        if (owner.TryGetResource(key, owner.ActualThemeVariant, out var val) && val is T typed)
            return typed;
        if (Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out val) == true && val is T typed2)
            return typed2;
        return null;
    }
}
