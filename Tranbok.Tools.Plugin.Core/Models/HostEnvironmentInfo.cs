namespace Tranbok.Tools.Plugin.Core;

public sealed record HostEnvironmentInfo(
    string ApplicationName,
    string ApplicationVersion,
    string RuntimeVersion,
    string TargetFramework,
    string Platform,
    string PluginApiVersion);
