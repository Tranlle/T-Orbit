using TOrbit.Core.Models;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Core.Services;

public sealed class AppShutdownCoordinator : IAppShutdownCoordinator
{
    private readonly IPluginCatalogService _pluginCatalogService;
    private readonly IAppDiagnosticsService _diagnosticsService;
    private int _hasShutdown;

    public AppShutdownCoordinator(IPluginCatalogService pluginCatalogService, IAppDiagnosticsService diagnosticsService)
    {
        _pluginCatalogService = pluginCatalogService;
        _diagnosticsService = diagnosticsService;
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _hasShutdown, 1) == 1)
            return;

        foreach (var entry in _pluginCatalogService.Plugins.Reverse().ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            await StopPluginAsync(entry, cancellationToken);
            await DisposePluginAsync(entry);
        }
    }

    private async Task StopPluginAsync(PluginEntry entry, CancellationToken cancellationToken)
    {
        if (entry.State is not (PluginState.Running or PluginState.Starting or PluginState.Stopping or PluginState.Faulted))
            return;

        try
        {
            await entry.Plugin.StopAsync(cancellationToken);
            entry.LastError = null;
        }
        catch (Exception ex)
        {
            entry.LastError = ex;
            _diagnosticsService.ReportError("PluginShutdown", $"Failed to stop plugin '{entry.Id}'.", ex);
        }
        finally
        {
            entry.NotifyStateChanged();
        }
    }

    private async Task DisposePluginAsync(PluginEntry entry)
    {
        try
        {
            await entry.Plugin.DisposeAsync();
            entry.LastError = null;
        }
        catch (Exception ex)
        {
            entry.LastError = ex;
            _diagnosticsService.ReportError("PluginShutdown", $"Failed to dispose plugin '{entry.Id}'.", ex);
        }
        finally
        {
            entry.NotifyStateChanged();
        }
    }
}
