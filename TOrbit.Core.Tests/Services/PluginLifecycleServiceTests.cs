using TOrbit.Core.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Enums;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class PluginLifecycleServiceTests
{
    [Fact]
    public async Task StartAsync_SerializesConcurrentCallsPerPlugin()
    {
        var catalog = new PluginCatalogService();
        var plugin = new ConcurrencyProbePlugin();
        catalog.Register(plugin);

        var service = new PluginLifecycleService(catalog, new PluginExecutionGate());

        await Task.WhenAll(
            service.StartAsync(plugin.Descriptor.Id),
            service.StartAsync(plugin.Descriptor.Id));

        Assert.Equal(1, plugin.MaxConcurrentStarts);
        Assert.Equal(2, plugin.StartCallCount);
    }

    private sealed class ConcurrencyProbePlugin : BasePlugin
    {
        private int _concurrentStarts;

        public int StartCallCount { get; private set; }
        public int MaxConcurrentStarts { get; private set; }

        public override PluginDescriptor Descriptor { get; } = new(
            "torbit.test.lifecycle",
            "Lifecycle Test",
            "1.0.0",
            "LifecycleTest.dll",
            "LifecycleTest.Plugin",
            Kind: PluginKind.Service);

        protected override async ValueTask OnStartAsync(CancellationToken cancellationToken = default)
        {
            StartCallCount++;
            var concurrent = Interlocked.Increment(ref _concurrentStarts);
            MaxConcurrentStarts = Math.Max(MaxConcurrentStarts, concurrent);

            try
            {
                await Task.Delay(50, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentStarts);
            }
        }
    }
}
