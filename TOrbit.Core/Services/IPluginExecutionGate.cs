namespace TOrbit.Core.Services;

public interface IPluginExecutionGate
{
    Task ExecuteAsync(string pluginId, Func<Task> operation, CancellationToken cancellationToken = default);
}
