namespace TOrbit.Plugin.RuntimeHost.Models;

public sealed class HostedAppLogEntry
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    public string ProfileId { get; set; } = string.Empty;

    public string Level { get; set; } = "Info";

    public string Message { get; set; } = string.Empty;
}
