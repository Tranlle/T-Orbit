using System.Collections.Concurrent;

namespace TOrbit.Core.Services;

public sealed class PluginExecutionGate : IPluginExecutionGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _pluginGates = new(StringComparer.OrdinalIgnoreCase);

    public async Task ExecuteAsync(string pluginId, Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var gate = _pluginGates.GetOrAdd(pluginId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await operation();
        }
        finally
        {
            gate.Release();
        }
    }
}
