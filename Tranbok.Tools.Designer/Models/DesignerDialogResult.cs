namespace Tranbok.Tools.Designer.Models;

public enum DesignerDialogResultKind
{
    None,
    Confirmed,
    Cancelled
}

public sealed class DesignerDialogResult<T>
{
    public DesignerDialogResultKind Kind { get; init; }
    public T? Value { get; init; }

    public bool IsConfirmed => Kind == DesignerDialogResultKind.Confirmed;
    public bool IsCancelled => Kind == DesignerDialogResultKind.Cancelled;

    public static DesignerDialogResult<T> Confirmed(T? value = default) => new()
    {
        Kind = DesignerDialogResultKind.Confirmed,
        Value = value
    };

    public static DesignerDialogResult<T> Cancelled(T? value = default) => new()
    {
        Kind = DesignerDialogResultKind.Cancelled,
        Value = value
    };
}
