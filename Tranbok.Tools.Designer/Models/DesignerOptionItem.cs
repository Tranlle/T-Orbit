namespace Tranbok.Tools.Designer.Models;

public sealed class DesignerOptionItem
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Description { get; init; }
    public object? Value { get; init; }
    public bool IsEnabled { get; init; } = true;

    public override string ToString() => Label;
}
