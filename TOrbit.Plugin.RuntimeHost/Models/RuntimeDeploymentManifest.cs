namespace TOrbit.Plugin.RuntimeHost.Models;

public sealed class RuntimeDeploymentManifest
{
    public string PackageFileName { get; set; } = string.Empty;

    public string PackageHash { get; set; } = string.Empty;

    public DateTimeOffset DeployedAt { get; set; } = DateTimeOffset.Now;

    public string EntryRelativePath { get; set; } = string.Empty;

    public bool RunWithDotnet { get; set; }
}
