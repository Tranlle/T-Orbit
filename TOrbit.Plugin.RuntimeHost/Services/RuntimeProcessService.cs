using System.Collections.Concurrent;
using System.Diagnostics;
using TOrbit.Designer.Services;
using TOrbit.Plugin.RuntimeHost.Models;

namespace TOrbit.Plugin.RuntimeHost.Services;

public sealed class RuntimeProcessService : IDisposable
{
    private readonly ConcurrentDictionary<string, RuntimeProcessHandle> _handles = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILocalizationService _localizationService;

    public RuntimeProcessService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public event EventHandler<HostedAppLogEntry>? LogReceived;

    public event EventHandler<HostedAppRuntimeState>? StateChanged;

    public HostedAppRuntimeState GetState(string profileId)
        => _handles.TryGetValue(profileId, out var handle)
            ? handle.State
            : new HostedAppRuntimeState { ProfileId = profileId };

    public async Task<HostedAppRuntimeState> StartAsync(RuntimeStartRequest request, CancellationToken cancellationToken = default)
    {
        await StopAsync(request.ProfileId, cancellationToken);

        var entryPath = Path.Combine(request.WorkingDirectory, request.EntryRelativePath);
        if (!File.Exists(entryPath))
            throw new FileNotFoundException(_localizationService.GetString("runtime.messages.entryFileNotFound"), entryPath);

        var state = new HostedAppRuntimeState
        {
            ProfileId = request.ProfileId,
            Status = RuntimeAppStatus.Starting
        };
        RaiseStateChanged(state);

        var startInfo = new ProcessStartInfo
        {
            FileName = request.RunWithDotnet ? "dotnet" : entryPath,
            Arguments = request.RunWithDotnet ? $"\"{entryPath}\"" : string.Empty,
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var variable in request.EnvironmentVariables.Where(v => !string.IsNullOrWhiteSpace(v.Key)))
            startInfo.Environment[variable.Key.Trim()] = variable.Value ?? string.Empty;

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        var handle = new RuntimeProcessHandle(process, state);
        if (!_handles.TryAdd(request.ProfileId, handle))
            throw new InvalidOperationException(_localizationService.GetString("runtime.messages.registerProcessFailed"));

        process.OutputDataReceived += (_, args) => OnOutput(request.ProfileId, "Info", args.Data);
        process.ErrorDataReceived += (_, args) => OnOutput(request.ProfileId, "Error", args.Data);
        process.Exited += (_, _) => OnExited(request.ProfileId);

        try
        {
            if (!process.Start())
                throw new InvalidOperationException(_localizationService.GetString("runtime.messages.processDidNotStart"));

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            state.ProcessId = process.Id;
            state.Status = RuntimeAppStatus.Running;
            state.StartedAt = DateTimeOffset.Now;
            state.ExitedAt = null;
            state.LastExitCode = null;
            state.LastError = string.Empty;

            RaiseStateChanged(state);
            OnOutput(
                request.ProfileId,
                "Info",
                string.Format(_localizationService.GetString("runtime.messages.processStartedWithPid"), process.Id));
            return CloneState(state);
        }
        catch
        {
            _handles.TryRemove(request.ProfileId, out _);
            process.Dispose();
            throw;
        }
    }

    public async Task<HostedAppRuntimeState> StopAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (!_handles.TryGetValue(profileId, out var handle))
            return new HostedAppRuntimeState { ProfileId = profileId, Status = RuntimeAppStatus.Stopped };

        var state = handle.State;
        if (handle.Process.HasExited)
        {
            _handles.TryRemove(profileId, out _);
            handle.Dispose();
            state.Status = RuntimeAppStatus.Stopped;
            state.ProcessId = null;
            RaiseStateChanged(state);
            return CloneState(state);
        }

        state.Status = RuntimeAppStatus.Stopping;
        RaiseStateChanged(state);
        OnOutput(profileId, "Info", _localizationService.GetString("runtime.messages.stoppingProcess"));

        try
        {
            if (!handle.Process.CloseMainWindow())
                handle.Process.Kill(entireProcessTree: true);
            else
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
                await handle.Process.WaitForExitAsync(timeoutCts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (!handle.Process.HasExited)
                handle.Process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }

        if (!handle.Process.HasExited)
            await handle.Process.WaitForExitAsync(cancellationToken);

        return CloneState(handle.State);
    }

    public async Task<HostedAppRuntimeState> RestartAsync(RuntimeStartRequest request, CancellationToken cancellationToken = default)
    {
        await StopAsync(request.ProfileId, cancellationToken);
        return await StartAsync(request, cancellationToken);
    }

    private void OnExited(string profileId)
    {
        if (!_handles.TryRemove(profileId, out var handle))
            return;

        var exitCode = 0;
        try
        {
            exitCode = handle.Process.ExitCode;
        }
        catch
        {
        }

        handle.State.ProcessId = null;
        handle.State.ExitedAt = DateTimeOffset.Now;
        handle.State.LastExitCode = exitCode;
        handle.State.Status = exitCode == 0 ? RuntimeAppStatus.Stopped : RuntimeAppStatus.Faulted;
        if (exitCode != 0)
            handle.State.LastError = string.Format(_localizationService.GetString("runtime.messages.processExitedWithCode"), exitCode);

        OnOutput(
            profileId,
            exitCode == 0 ? "Info" : "Error",
            string.Format(_localizationService.GetString("runtime.messages.processExitedWithCode"), exitCode));
        RaiseStateChanged(handle.State);
        handle.Dispose();
    }

    private void OnOutput(string profileId, string level, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        LogReceived?.Invoke(this, new HostedAppLogEntry
        {
            ProfileId = profileId,
            Level = level,
            Message = message.Trim()
        });
    }

    private void RaiseStateChanged(HostedAppRuntimeState state)
        => StateChanged?.Invoke(this, CloneState(state));

    private static HostedAppRuntimeState CloneState(HostedAppRuntimeState state)
    {
        return new HostedAppRuntimeState
        {
            ProfileId = state.ProfileId,
            Status = state.Status,
            ProcessId = state.ProcessId,
            StartedAt = state.StartedAt,
            ExitedAt = state.ExitedAt,
            LastDeployAt = state.LastDeployAt,
            LastExitCode = state.LastExitCode,
            LastError = state.LastError
        };
    }

    public void Dispose()
    {
        foreach (var handle in _handles.Values)
        {
            try
            {
                if (!handle.Process.HasExited)
                    handle.Process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            handle.Dispose();
        }

        _handles.Clear();
    }

    private sealed class RuntimeProcessHandle : IDisposable
    {
        public RuntimeProcessHandle(Process process, HostedAppRuntimeState state)
        {
            Process = process;
            State = state;
        }

        public Process Process { get; }

        public HostedAppRuntimeState State { get; }

        public void Dispose() => Process.Dispose();
    }
}
