namespace TOrbit.Plugin.SubtitleTranslator.Models;

public sealed record TranslationResult(
    string SourceText,
    string TranslatedText,
    string TargetLanguage,
    TimeSpan Duration,
    bool IsSuccess,
    string? ErrorMessage = null);
