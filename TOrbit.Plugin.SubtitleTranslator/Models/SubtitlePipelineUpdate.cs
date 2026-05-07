namespace TOrbit.Plugin.SubtitleTranslator.Models;

public sealed record SubtitlePipelineUpdate(
    string StatusText,
    string PipelineStateText,
    string OcrStatusText,
    string TranslationStatusText,
    string? SourceText = null,
    string? TranslatedText = null,
    string? LastUpdatedText = null,
    string? WorkspaceHint = null,
    string? LogMessage = null);
