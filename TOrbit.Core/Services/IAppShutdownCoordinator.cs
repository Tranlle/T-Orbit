namespace TOrbit.Core.Services;

public interface IAppShutdownCoordinator
{
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
