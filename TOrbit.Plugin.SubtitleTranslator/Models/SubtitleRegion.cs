namespace TOrbit.Plugin.SubtitleTranslator.Models;

public sealed record SubtitleRegion(
    int X,
    int Y,
    int Width,
    int Height)
{
    public bool IsValid => Width > 0 && Height > 0;

    public override string ToString() => IsValid
        ? $"{Width} x {Height} @ ({X}, {Y})"
        : "No region selected";
}
