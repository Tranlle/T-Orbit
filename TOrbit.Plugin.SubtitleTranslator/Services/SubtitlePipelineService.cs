using System.Runtime.Versioning;
using SkiaSharp;
using TOrbit.Designer.Services;
using TOrbit.Plugin.SubtitleTranslator.Models;

namespace TOrbit.Plugin.SubtitleTranslator.Services;

[SupportedOSPlatform("windows")]
public sealed class SubtitlePipelineService : IDisposable
{
    private readonly ILocalizationService _localizationService;
    private readonly ScreenCaptureService _screenCaptureService = new();
    private readonly ImagePreprocessService _imagePreprocessService = new();
    private readonly LocalOcrService _localOcrService = new();
    private readonly SubtitleStabilizerService _subtitleStabilizerService = new();
    private readonly TranslationService _translationService = new();
    private readonly FrameChangeGateService _frameChangeGateService = new();
    private SubtitleTranslatorVariables _variables = new();

    public SubtitlePipelineService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public void UpdateVariables(SubtitleTranslatorVariables variables)
    {
        _variables = variables;
        _translationService.Configure(new TranslationServiceOptions(
            Provider: EffectiveProvider(variables),
            TargetLanguage: string.IsNullOrWhiteSpace(variables.TargetLanguage) ? "zh-CN" : variables.TargetLanguage,
            Model: variables.TranslationModel,
            Endpoint: variables.TranslationEndpoint,
            ApiKey: variables.TranslationApiKey));
    }

    public async Task RunAsync(SubtitleRegion region, Action<SubtitlePipelineUpdate> onUpdate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onUpdate);

        onUpdate(new SubtitlePipelineUpdate(
            StatusText: L("subtitle.status.running"),
            PipelineStateText: L("subtitle.state.sampling"),
            OcrStatusText: L("subtitle.state.waiting"),
            TranslationStatusText: BuildTranslationStatus(),
            WorkspaceHint: L("subtitle.hint.running"),
            LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.captureStarted")} {CaptureIntervalMs} ms"));

        onUpdate(new SubtitlePipelineUpdate(
            StatusText: L("subtitle.status.running"),
            PipelineStateText: L("subtitle.state.sampling"),
            OcrStatusText: L("subtitle.state.waiting"),
            TranslationStatusText: BuildTranslationStatus(),
            LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.regionReady")}: {region}"));

        while (!cancellationToken.IsCancellationRequested)
        {
            await RunSingleCycleAsync(region, onUpdate, cancellationToken);
            await Task.Delay(CaptureIntervalMs, cancellationToken);
        }
    }

    public void Reset()
    {
        _subtitleStabilizerService.Reset();
        _frameChangeGateService.Reset();
    }

    public void Dispose()
    {
        _localOcrService.Dispose();
        _subtitleStabilizerService.Reset();
        _frameChangeGateService.Reset();
    }

    private async Task RunSingleCycleAsync(SubtitleRegion region, Action<SubtitlePipelineUpdate> onUpdate, CancellationToken cancellationToken)
    {
        using var capturedBitmap = await _screenCaptureService.CaptureAsync(region, cancellationToken);
        onUpdate(new SubtitlePipelineUpdate(
            StatusText: L("subtitle.status.running"),
            PipelineStateText: L("subtitle.state.sampling"),
            OcrStatusText: L("subtitle.state.evaluatingFrame"),
            TranslationStatusText: BuildTranslationStatus(),
            LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.frameCaptured")}: {capturedBitmap.Width} x {capturedBitmap.Height}"));

        var changeDecision = _frameChangeGateService.Evaluate(
            capturedBitmap,
            _variables.EnableFrameChangeDetection,
            FrameChangeThreshold);
        if (changeDecision.ShouldSkipOcr)
        {
            onUpdate(new SubtitlePipelineUpdate(
                StatusText: L("subtitle.status.frameUnchanged"),
                PipelineStateText: L("subtitle.state.sampling"),
                OcrStatusText: L("subtitle.state.skippedUnchanged"),
                TranslationStatusText: BuildTranslationStatus(),
                WorkspaceHint: L("subtitle.hint.frameSkipped"),
                LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.frameSkipped")} (delta {changeDecision.Delta:F3}, threshold {FrameChangeThreshold:F3})"));
            return;
        }

        if (changeDecision.Delta >= 0)
        {
            onUpdate(new SubtitlePipelineUpdate(
                StatusText: L("subtitle.status.running"),
                PipelineStateText: L("subtitle.state.sampling"),
                OcrStatusText: L("subtitle.state.rawOcrRunning"),
                TranslationStatusText: BuildTranslationStatus(),
                LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.frameAccepted")} (delta {changeDecision.Delta:F3}, threshold {FrameChangeThreshold:F3})"));
        }

        var rawResult = await _localOcrService.RecognizeAsync(capturedBitmap, cancellationToken);

        var preprocessOptions = BuildPreprocessOptions();
        using var processedBitmap = _imagePreprocessService.Preprocess(capturedBitmap, preprocessOptions);
        onUpdate(new SubtitlePipelineUpdate(
            StatusText: L("subtitle.status.running"),
            PipelineStateText: L("subtitle.state.sampling"),
            OcrStatusText: rawResult.IsSuccess ? L("subtitle.state.rawOcrCompleted") : L("subtitle.state.rawOcrFailed"),
            TranslationStatusText: BuildTranslationStatus(),
            LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.preprocessApplied")}: pad {preprocessOptions.Padding}, scale {preprocessOptions.ScaleFactor:F1}x, contrast {preprocessOptions.Contrast:F2}, threshold {preprocessOptions.Threshold}"));

        var processedResult = await _localOcrService.RecognizeAsync(processedBitmap, cancellationToken);
        onUpdate(new SubtitlePipelineUpdate(
            StatusText: processedResult.IsSuccess ? L("subtitle.status.ocrPassCompleted") : L("subtitle.status.ocrFailed"),
            PipelineStateText: L("subtitle.state.sampling"),
            OcrStatusText: processedResult.IsSuccess ? L("subtitle.state.ocrReady") : L("subtitle.state.ocrFailed"),
            TranslationStatusText: BuildTranslationStatus(),
            LastUpdatedText: processedResult.IsSuccess ? $"{L("subtitle.state.updatedAt")} {processedResult.CapturedAt:HH:mm:ss}" : L("subtitle.status.ocrFailed"),
            LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.ocrCompare")}: raw=\"{Summarize(rawResult.RawText)}\" | processed=\"{Summarize(processedResult.RawText)}\""));

        if (!processedResult.IsSuccess)
        {
            onUpdate(new SubtitlePipelineUpdate(
                StatusText: L("subtitle.status.ocrFailed"),
                PipelineStateText: L("subtitle.state.sampling"),
                OcrStatusText: L("subtitle.state.ocrFailed"),
                TranslationStatusText: BuildTranslationStatus(),
                SourceText: L("subtitle.error.ocrFrameFailed"),
                WorkspaceHint: L("subtitle.hint.ocrFailed"),
                LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.ocrError")}: {processedResult.ErrorMessage}"));
            return;
        }

        var frameState = _subtitleStabilizerService.Process(
            processedResult.RawText,
            processedResult.CapturedAt,
            BuildStabilizerOptions());

        onUpdate(new SubtitlePipelineUpdate(
            StatusText: frameState.IsNewSubtitle ? L("subtitle.status.ocrPassCompleted") : L("subtitle.status.duplicateSuppressed"),
            PipelineStateText: L("subtitle.state.sampling"),
            OcrStatusText: L("subtitle.state.ocrReady"),
            TranslationStatusText: BuildTranslationStatus(),
            WorkspaceHint: L("subtitle.hint.stabilizing"),
            LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.stabilizer")}: similarity {frameState.Similarity:F2}, update={(frameState.IsNewSubtitle ? L("subtitle.log.booleanYes") : L("subtitle.log.booleanNo"))}, translate={(frameState.ShouldTranslate ? L("subtitle.log.booleanYes") : L("subtitle.log.booleanNo"))}"));

        if (frameState.IsNewSubtitle)
        {
            onUpdate(new SubtitlePipelineUpdate(
                StatusText: L("subtitle.status.ocrPassCompleted"),
                PipelineStateText: L("subtitle.state.sampling"),
                OcrStatusText: L("subtitle.state.ocrReady"),
                TranslationStatusText: BuildTranslationStatus(),
                SourceText: string.IsNullOrWhiteSpace(frameState.StableText)
                    ? L("subtitle.error.noReadableText")
                    : frameState.StableText,
                LogMessage: string.IsNullOrWhiteSpace(frameState.StableText)
                    ? null
                    : $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.stableText")}: {frameState.StableText}"));
        }

        onUpdate(new SubtitlePipelineUpdate(
            StatusText: frameState.IsNewSubtitle ? L("subtitle.status.ocrPassCompleted") : L("subtitle.status.duplicateSuppressed"),
            PipelineStateText: L("subtitle.state.sampling"),
            OcrStatusText: L("subtitle.state.ocrReady"),
            TranslationStatusText: BuildTranslationStatus(),
            LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.ocrSuccess")} {processedResult.Duration.TotalMilliseconds:F0} ms, confidence {processedResult.Confidence:F2}"));

        if (!frameState.ShouldTranslate || string.IsNullOrWhiteSpace(frameState.StableText))
            return;

        var translationResult = await _translationService.TranslateAsync(frameState.StableText, cancellationToken);
        if (translationResult.IsSuccess)
        {
            onUpdate(new SubtitlePipelineUpdate(
                StatusText: L("subtitle.status.ocrPassCompleted"),
                PipelineStateText: L("subtitle.state.sampling"),
                OcrStatusText: L("subtitle.state.ocrReady"),
                TranslationStatusText: $"{L("subtitle.translation.translatedTo")} {translationResult.TargetLanguage}",
                TranslatedText: translationResult.TranslatedText,
                LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.translationUpdated")} {translationResult.Duration.TotalMilliseconds:F0} ms"));
            return;
        }

        onUpdate(new SubtitlePipelineUpdate(
            StatusText: L("subtitle.status.ocrPassCompleted"),
            PipelineStateText: L("subtitle.state.sampling"),
            OcrStatusText: L("subtitle.state.ocrReady"),
            TranslationStatusText: BuildTranslationFailureStatus(),
            TranslatedText: $"{L("subtitle.translation.originalOnly")} {translationResult.ErrorMessage}",
            LogMessage: $"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.translationSkipped")}: {translationResult.ErrorMessage}"));
    }

    private int CaptureIntervalMs => Math.Clamp(_variables.CaptureIntervalMs, 150, 5000);

    private double FrameChangeThreshold => Math.Clamp(_variables.FrameChangeThreshold, 0, 1);

    private ImagePreprocessOptions BuildPreprocessOptions() => new(
        Padding: Math.Clamp(_variables.OcrPadding, 0, 200),
        ScaleFactor: 1.8f,
        Contrast: 1.35f,
        EnableThreshold: true,
        Threshold: 148);

    private SubtitleStabilizerOptions BuildStabilizerOptions() => new(
        SimilarityThreshold: Math.Clamp(_variables.SimilarityThreshold, 0, 1),
        MinRefreshInterval: TimeSpan.FromMilliseconds(Math.Clamp(_variables.MinRefreshIntervalMs, 0, 10000)),
        StabilizationWindow: TimeSpan.FromMilliseconds(Math.Clamp(_variables.StabilizationWindowMs, 0, 10000)));

    private string BuildTranslationStatus()
        => string.Equals(EffectiveProvider(_variables), TranslationProviders.Disabled, StringComparison.OrdinalIgnoreCase)
            ? L("subtitle.translation.notConfigured")
            : $"{L("subtitle.translation.providerPrefix")}: {EffectiveProvider(_variables)}";

    private string BuildTranslationFailureStatus()
        => string.Equals(EffectiveProvider(_variables), TranslationProviders.Disabled, StringComparison.OrdinalIgnoreCase)
            ? L("subtitle.translation.notConfigured")
            : $"{L("subtitle.translation.providerIssue")}: {EffectiveProvider(_variables)}";

    private static string EffectiveProvider(SubtitleTranslatorVariables variables)
        => string.IsNullOrWhiteSpace(variables.TranslationProvider)
            ? TranslationProviders.Disabled
            : variables.TranslationProvider;

    private static string Summarize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "<empty>";

        var normalized = text.Replace(Environment.NewLine, " ").Trim();
        return normalized.Length <= 48 ? normalized : $"{normalized[..48]}...";
    }

    private string L(string key) => _localizationService.GetString(key);
}
