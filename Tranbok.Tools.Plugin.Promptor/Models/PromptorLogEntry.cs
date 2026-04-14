using System.Text;

namespace Tranbok.Tools.Plugin.Promptor.Models;

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
        const string sep = "──────────────────────────────────────────────────────";
        var sb = new StringBuilder();
        sb.AppendLine(sep);
        sb.AppendLine($"  时间   {Time:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  策略   {StrategyLabel}");
        sb.AppendLine($"  模型   {Model}  ({Provider})");
        sb.AppendLine($"  耗时   {Duration.TotalSeconds:F2} 秒");
        sb.AppendLine($"  状态   {(IsSuccess ? "✓ 成功" : "✗ 失败")}");
        if (!string.IsNullOrEmpty(ErrorMessage))
            sb.AppendLine($"  错误   {ErrorMessage}");
        if (!string.IsNullOrEmpty(InputPreview))
            sb.AppendLine($"  输入   {InputPreview}");
        sb.Append(sep);
        return sb.ToString();
    }
}
