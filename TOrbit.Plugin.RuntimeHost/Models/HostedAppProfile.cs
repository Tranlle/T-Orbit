namespace TOrbit.Plugin.RuntimeHost.Models;

public sealed class HostedAppProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string DeployFolderName { get; set; } = string.Empty;

    public string PackagePath { get; set; } = string.Empty;

    public string EntryRelativePath { get; set; } = string.Empty;

    public bool RunWithDotnet { get; set; }

    public bool AutoStart { get; set; }

    public List<HostedAppEnvironmentVariable> EnvironmentVariables { get; set; } = [];
}
