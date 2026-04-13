using Tranbok.Tools.Plugin.Core.Enums;

namespace Tranbok.Tools.Plugin.Core;

public sealed record PluginCompatibilityResult(
    PluginCompatibilityStatus Status,
    string? Message = null,
    IReadOnlyCollection<string>? Details = null);
