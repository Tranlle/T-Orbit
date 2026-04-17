using TOrbit.Core.Constants;

namespace TOrbit.Core.Services;

public sealed class AppStartupCoordinator : IAppStartupCoordinator
{
    private readonly IPluginDiscoveryService _pluginDiscoveryService;
    private readonly IPluginVariableService _pluginVariableService;
    private readonly IKeyMapService _keyMapService;
    private readonly IAppDiagnosticsService _diagnosticsService;
    private readonly IHomeReportRegistrationService _homeReportRegistrationService;
    private readonly SemaphoreSlim _startupGate = new(1, 1);
    private bool _hasStarted;

    public AppStartupCoordinator(
        IPluginDiscoveryService pluginDiscoveryService,
        IPluginVariableService pluginVariableService,
        IKeyMapService keyMapService,
        IAppDiagnosticsService diagnosticsService,
        IHomeReportRegistrationService homeReportRegistrationService)
    {
        _pluginDiscoveryService = pluginDiscoveryService;
        _pluginVariableService = pluginVariableService;
        _keyMapService = keyMapService;
        _diagnosticsService = diagnosticsService;
        _homeReportRegistrationService = homeReportRegistrationService;
    }

    public async Task WarmupAsync(Func<CancellationToken, Task>? registerBuiltIns = null, CancellationToken cancellationToken = default)
    {
        await _startupGate.WaitAsync(cancellationToken);
        try
        {
            if (_hasStarted)
                return;

            if (registerBuiltIns is not null)
                await RunStepAsync("BuiltInPlugins", () => registerBuiltIns(cancellationToken));

            await RunPluginDiscoveryAsync(cancellationToken);
            await RunStepAsync("PluginVariables", () =>
            {
                _pluginVariableService.InjectAll();
                return Task.CompletedTask;
            });
            await RunStepAsync("KeyMap", () =>
            {
                _keyMapService.Load();
                return Task.CompletedTask;
            });
            await RunStepAsync("HomeReports", () =>
            {
                _homeReportRegistrationService.Initialize();
                return Task.CompletedTask;
            });

            _hasStarted = true;
        }
        finally
        {
            _startupGate.Release();
        }
    }

    private async Task RunPluginDiscoveryAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pluginDiscoveryService.LoadAsync(
                Path.Combine(AppContext.BaseDirectory, ToolHostConstants.PluginsDirectoryName),
                cancellationToken);

            foreach (var error in result.Errors)
            {
                var target = string.IsNullOrWhiteSpace(error.PluginId)
                    ? error.AssemblyPath
                    : $"{error.PluginId} ({error.AssemblyPath})";
                _diagnosticsService.ReportError("PluginDiscovery", $"Failed to load plugin: {target}. {error.Message}", error.Exception);
            }
        }
        catch (Exception ex)
        {
            _diagnosticsService.ReportError("PluginDiscovery", "Plugin discovery failed during application startup.", ex);
        }
    }

    private async Task RunStepAsync(string source, Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            _diagnosticsService.ReportError(source, $"Startup step '{source}' failed.", ex);
        }
    }
}
