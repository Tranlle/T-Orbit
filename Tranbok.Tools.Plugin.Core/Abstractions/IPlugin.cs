namespace Tranbok.Tools.Plugin.Core.Abstractions;

public interface IPlugin : IAsyncDisposable
{
    PluginDescriptor Descriptor { get; }

    ValueTask InitializeAsync(PluginContext context, CancellationToken cancellationToken = default);

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
