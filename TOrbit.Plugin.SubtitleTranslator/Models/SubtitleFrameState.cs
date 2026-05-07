namespace TOrbit.Plugin.SubtitleTranslator.Models;

public sealed record SubtitleFrameState(
    string StableText,
    bool IsNewSubtitle,
    double Similarity,
    bool ShouldTranslate,
    DateTimeOffset UpdatedAt);
