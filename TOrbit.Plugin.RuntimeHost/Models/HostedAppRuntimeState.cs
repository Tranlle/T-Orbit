namespace TOrbit.Plugin.RuntimeHost.Models;

public sealed class HostedAppRuntimeState
{
    public string ProfileId { get; set; } = string.Empty;

    public RuntimeAppStatus Status { get; set; } = RuntimeAppStatus.Stopped;

    public int? ProcessId { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? ExitedAt { get; set; }

    public DateTimeOffset? LastDeployAt { get; set; }

    public int? LastExitCode { get; set; }

    public string LastError { get; set; } = string.Empty;
}
