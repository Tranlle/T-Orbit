using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using TOrbit.Plugin.SubtitleTranslator.Models;
using TOrbit.Plugin.SubtitleTranslator.Views;

namespace TOrbit.Plugin.SubtitleTranslator.Services;

public sealed class RegionSelectionService
{
    public async Task<SubtitleRegion?> SelectRegionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var owner = TryGetOwnerWindow();
        if (owner is null)
            throw new InvalidOperationException("Unable to open the selection overlay because the main window is not available.");

        var screen = owner.Screens?.ScreenFromWindow(owner) ?? owner.Screens?.Primary;
        if (screen is null)
            throw new InvalidOperationException("Unable to resolve the current display for region selection.");

        var bounds = screen.Bounds;
        var overlay = new RegionSelectionWindow
        {
            Width = bounds.Width,
            Height = bounds.Height,
            Position = bounds.Position
        };

        return await overlay.ShowDialog<SubtitleRegion?>(owner);
    }

    private static Window? TryGetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }
}
