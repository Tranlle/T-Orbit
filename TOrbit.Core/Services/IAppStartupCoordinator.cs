namespace TOrbit.Core.Services;

public interface IAppStartupCoordinator
{
    Task WarmupAsync(Func<CancellationToken, Task>? registerBuiltIns = null, CancellationToken cancellationToken = default);
}
