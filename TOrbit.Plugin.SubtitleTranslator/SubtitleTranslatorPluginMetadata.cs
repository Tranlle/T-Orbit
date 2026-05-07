using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.SubtitleTranslator.Services;

namespace TOrbit.Plugin.SubtitleTranslator;

public sealed class SubtitleTranslatorPluginMetadata : PluginBaseMetadata
{
    public static SubtitleTranslatorPluginMetadata Instance { get; } = new();

    public override string Id => "torbit.subtitle-translator";
    public override string Name => "Subtitle Translator";
    public override string Version => "1.0.0";
    public override string Description => "Realtime subtitle OCR workspace";
    public override string Author => "T-Orbit";
    public override string Icon => "Translate";
    public override string Tags => "OCR,Subtitle,Translation";
    public override IReadOnlyList<PluginCapability> Capabilities =>
    [
        PluginCapability.Network,
        PluginCapability.Secrets
    ];

    public override IReadOnlyList<PluginVariableDefinition> VariableDefinitions =>
    [
        new(
            Key: "SUBTITLE_TARGET_LANGUAGE",
            DefaultValue: "zh-CN",
            DisplayName: "Target Language",
            Description: "Translated subtitle target language",
            IsRequired: true),
        new(
            Key: "SUBTITLE_CAPTURE_INTERVAL_MS",
            DefaultValue: "500",
            DisplayName: "Capture Interval",
            Description: "Timed capture interval in milliseconds",
            IsRequired: true,
            ValidationPattern: @"^\d+$",
            ValidationMessage: "Must be a positive integer"),
        new(
            Key: "SUBTITLE_SHOW_SOURCE_TEXT",
            DefaultValue: "true",
            DisplayName: "Show Source Text",
            Description: "Whether to show recognized subtitle text",
            AllowedValues: ["true", "false"]),
        new(
            Key: "SUBTITLE_SHOW_TRANSLATED_TEXT",
            DefaultValue: "true",
            DisplayName: "Show Translated Text",
            Description: "Whether to show translated subtitle text",
            AllowedValues: ["true", "false"]),
        new(
            Key: "SUBTITLE_OCR_PADDING",
            DefaultValue: "20",
            DisplayName: "OCR Padding",
            Description: "Padding added around the subtitle region before OCR",
            ValidationPattern: @"^\d+$",
            ValidationMessage: "Must be a non-negative integer"),
        new(
            Key: "SUBTITLE_SIMILARITY_THRESHOLD",
            DefaultValue: "0.92",
            DisplayName: "Similarity Threshold",
            Description: "Similarity above this value is treated as duplicate",
            ValidationPattern: @"^(?:0(?:\.\d+)?|1(?:\.0+)?)$",
            ValidationMessage: "Must be between 0 and 1"),
        new(
            Key: "SUBTITLE_MIN_REFRESH_INTERVAL_MS",
            DefaultValue: "600",
            DisplayName: "Min Refresh Interval",
            Description: "Minimum time between duplicate subtitle updates",
            ValidationPattern: @"^\d+$",
            ValidationMessage: "Must be a positive integer"),
        new(
            Key: "SUBTITLE_STABILIZATION_WINDOW_MS",
            DefaultValue: "900",
            DisplayName: "Stabilization Window",
            Description: "Allowed window for incremental subtitle growth",
            ValidationPattern: @"^\d+$",
            ValidationMessage: "Must be a positive integer"),
        new(
            Key: "SUBTITLE_ENABLE_FRAME_CHANGE_DETECTION",
            DefaultValue: "true",
            DisplayName: "Enable Frame Change Detection",
            Description: "Skip OCR when the subtitle region is visually unchanged",
            AllowedValues: ["true", "false"]),
        new(
            Key: "SUBTITLE_FRAME_CHANGE_THRESHOLD",
            DefaultValue: "0.035",
            DisplayName: "Frame Change Threshold",
            Description: "Minimum sampled visual change required to trigger OCR",
            ValidationPattern: @"^(?:0(?:\.\d+)?|1(?:\.0+)?)$",
            ValidationMessage: "Must be between 0 and 1"),
        new(
            Key: "SUBTITLE_TRANSLATION_PROVIDER",
            DefaultValue: TranslationProviders.Disabled,
            DisplayName: "Translation Provider",
            Description: "Translation backend",
            AllowedValues: [TranslationProviders.Disabled, TranslationProviders.Echo, TranslationProviders.OpenAiCompatible]),
        new(
            Key: "SUBTITLE_TRANSLATION_MODEL",
            DefaultValue: string.Empty,
            DisplayName: "Translation Model",
            Description: "Model name used by the translation provider"),
        new(
            Key: "SUBTITLE_TRANSLATION_ENDPOINT",
            DefaultValue: string.Empty,
            DisplayName: "Translation Endpoint",
            Description: "Optional translation API endpoint"),
        new(
            Key: "SUBTITLE_TRANSLATION_API_KEY",
            DefaultValue: string.Empty,
            DisplayName: "Translation API Key",
            Description: "Optional translation provider credential",
            IsEncrypted: true)
    ];
}
