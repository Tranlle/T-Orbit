using System.Text;

namespace TOrbit.Plugin.Promptor.Models;

public sealed class PromptorLogEntry
{
    public required DateTime Time { get; init; }
    public required string StrategyLabel { get; init; }
    public required string Model { get; init; }
    public required string Provider { get; init; }
    public required bool IsSuccess { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string InputPreview { get; init; }
    public string? ErrorMessage { get; init; }

    public string FormatAsText()
    {
        const string sep = "--------------------------------------------------------";
        var sb = new StringBuilder();
        sb.AppendLine(sep);
        sb.AppendLine($"  Time      {Time:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  Strategy  {StrategyLabel}");
        sb.AppendLine($"  Model     {Model} ({Provider})");
        sb.AppendLine($"  Duration  {Duration.TotalSeconds:F2}s");
        sb.AppendLine($"  Status    {(IsSuccess ? "Success" : "Failed")}");
        if (!string.IsNullOrEmpty(ErrorMessage))
            sb.AppendLine($"  Error     {ErrorMessage}");
        if (!string.IsNullOrEmpty(InputPreview))
            sb.AppendLine($"  Input     {InputPreview}");
        sb.Append(sep);
        return sb.ToString();
    }
}
