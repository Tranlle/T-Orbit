using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.SubtitleTranslator.Models;

public sealed class SubtitleTranslatorVariables
{
    [PluginVariableKey("SUBTITLE_TARGET_LANGUAGE")]
    public string TargetLanguage { get; init; } = "zh-CN";

    [PluginVariableKey("SUBTITLE_CAPTURE_INTERVAL_MS")]
    public int CaptureIntervalMs { get; init; } = 500;

    [PluginVariableKey("SUBTITLE_SHOW_SOURCE_TEXT")]
    public bool ShowSourceText { get; init; } = true;

    [PluginVariableKey("SUBTITLE_SHOW_TRANSLATED_TEXT")]
    public bool ShowTranslatedText { get; init; } = true;

    [PluginVariableKey("SUBTITLE_OCR_PADDING")]
    public int OcrPadding { get; init; } = 24;

    [PluginVariableKey("SUBTITLE_OCR_MAX_SIDE_LEN")]
    public int OcrMaxSideLength { get; init; } = 1024;

    [PluginVariableKey("SUBTITLE_OCR_BOX_SCORE_THRESH")]
    public double OcrBoxScoreThreshold { get; init; } = 0.5;

    [PluginVariableKey("SUBTITLE_OCR_BOX_THRESH")]
    public double OcrBoxThreshold { get; init; } = 0.3;

    [PluginVariableKey("SUBTITLE_OCR_UNCLIP_RATIO")]
    public double OcrUnclipRatio { get; init; } = 1.6;

    [PluginVariableKey("SUBTITLE_OCR_DO_ANGLE")]
    public bool OcrDoAngle { get; init; } = true;

    [PluginVariableKey("SUBTITLE_OCR_MOST_ANGLE")]
    public bool OcrMostAngle { get; init; }

    [PluginVariableKey("SUBTITLE_SIMILARITY_THRESHOLD")]
    public double SimilarityThreshold { get; init; } = 0.92;

    [PluginVariableKey("SUBTITLE_MIN_REFRESH_INTERVAL_MS")]
    public int MinRefreshIntervalMs { get; init; } = 600;

    [PluginVariableKey("SUBTITLE_STABILIZATION_WINDOW_MS")]
    public int StabilizationWindowMs { get; init; } = 900;

    [PluginVariableKey("SUBTITLE_ENABLE_FRAME_CHANGE_DETECTION")]
    public bool EnableFrameChangeDetection { get; init; } = true;

    [PluginVariableKey("SUBTITLE_FRAME_CHANGE_THRESHOLD")]
    public double FrameChangeThreshold { get; init; } = 0.035;

    [PluginVariableKey("SUBTITLE_TRANSLATION_PROVIDER")]
    public string TranslationProvider { get; init; } = string.Empty;

    [PluginVariableKey("SUBTITLE_TRANSLATION_MODEL")]
    public string TranslationModel { get; init; } = string.Empty;

    [PluginVariableKey("SUBTITLE_TRANSLATION_ENDPOINT")]
    public string TranslationEndpoint { get; init; } = string.Empty;

    [PluginVariableKey("SUBTITLE_TRANSLATION_API_KEY")]
    public string TranslationApiKey { get; init; } = string.Empty;
}
