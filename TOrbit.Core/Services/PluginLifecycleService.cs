using System.Collections.Concurrent;
using TOrbit.Core.Models;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Core.Services;

public sealed class PluginLifecycleService : IPluginLifecycleService
{
    private readonly IPluginCatalogService _catalog;
    private readonly IPluginExecutionGate _executionGate;

    public PluginLifecycleService(IPluginCatalogService catalog, IPluginExecutionGate executionGate)
    {
        _catalog = catalog;
        _executionGate = executionGate;
    }

    public async Task StopAsync(string pluginId, CancellationToken ct = default)
    {
        await ExecuteSerializedAsync(pluginId, async entry =>
        {
            if (!entry.CanDisable)
                return;

            await entry.Plugin.StopAsync(ct);
        }, ct);
    }

    public async Task StartAsync(string pluginId, CancellationToken ct = default)
    {
        await ExecuteSerializedAsync(pluginId, entry => entry.Plugin.StartAsync(ct).AsTask(), ct);
    }

    public async Task RestartAsync(string pluginId, CancellationToken ct = default)
    {
        await ExecuteSerializedAsync(pluginId, async entry =>
        {
            if (!entry.CanDisable)
                return;

            await entry.Plugin.StopAsync(ct);
            await entry.Plugin.StartAsync(ct);
        }, ct);
    }

    private PluginEntry GetRequiredEntry(string pluginId)
        => _catalog.Get(pluginId) ?? throw new InvalidOperationException($"Plugin '{pluginId}' not found.");

    private async Task ExecuteSerializedAsync(
        string pluginId,
        Func<PluginEntry, Task> operation,
        CancellationToken ct)
    {
        var entry = GetRequiredEntry(pluginId);
        await _executionGate.ExecuteAsync(pluginId, async () =>
        {
            try
            {
                await operation(entry);
                entry.LastError = null;
            }
            catch (Exception ex)
            {
                entry.LastError = ex;
                SetFaulted(entry);
            }
            finally
            {
                entry.NotifyStateChanged();
            }
        }, ct);
    }

    private static void SetFaulted(PluginEntry entry)
    {
        if (entry.Plugin is BasePlugin basePlugin)
            basePlugin.SetState(PluginState.Faulted);
    }
}
