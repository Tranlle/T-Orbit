using RapidOcrNet;
using SkiaSharp;
using TOrbit.Plugin.SubtitleTranslator.Models;

namespace TOrbit.Plugin.SubtitleTranslator.Services;

public sealed class LocalOcrService : IDisposable
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private RapidOcr? _engine;
    private bool _isInitialized;
    private OcrModelPaths? _modelPaths;
    private int _intraOpThreads = 1;

    public bool IsInitialized => _isInitialized;

    public void ConfigureModels(OcrModelPaths modelPaths, int intraOpThreads = 1)
    {
        ArgumentNullException.ThrowIfNull(modelPaths);
        _modelPaths = modelPaths;
        _intraOpThreads = Math.Max(1, intraOpThreads);
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return;

            _engine = new RapidOcr();
            InitializeEngine(_engine);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _engine?.Dispose();
            _engine = null;
            throw new InvalidOperationException(
                "Failed to initialize the local OCR engine. Make sure the OCR model files are present and the configured paths are valid.",
                ex);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<OcrFrameResult> RecognizeAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        await EnsureInitializedAsync(cancellationToken);

        if (!File.Exists(imagePath))
        {
            return new OcrFrameResult(
                RawText: string.Empty,
                Confidence: 0,
                Duration: TimeSpan.Zero,
                CapturedAt: DateTimeOffset.Now,
                IsSuccess: false,
                ErrorMessage: $"Image file was not found: {imagePath}");
        }

        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap is null)
        {
            return new OcrFrameResult(
                RawText: string.Empty,
                Confidence: 0,
                Duration: TimeSpan.Zero,
                CapturedAt: DateTimeOffset.Now,
                IsSuccess: false,
                ErrorMessage: $"Unable to decode image file: {imagePath}");
        }

        return await RecognizeAsync(bitmap, cancellationToken);
    }

    public async Task<OcrFrameResult> RecognizeAsync(SKBitmap bitmap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        await EnsureInitializedAsync(cancellationToken);

        var capturedAt = DateTimeOffset.Now;
        var startedAt = DateTime.UtcNow;

        try
        {
            var result = _engine!.Detect(bitmap, RapidOcrOptions.Default);
            var duration = DateTime.UtcNow - startedAt;

            return new OcrFrameResult(
                RawText: result.StrRes?.Trim() ?? string.Empty,
                Confidence: TryGetAverageConfidence(result),
                Duration: duration,
                CapturedAt: capturedAt,
                IsSuccess: true);
        }
        catch (Exception ex)
        {
            return new OcrFrameResult(
                RawText: string.Empty,
                Confidence: 0,
                Duration: DateTime.UtcNow - startedAt,
                CapturedAt: capturedAt,
                IsSuccess: false,
                ErrorMessage: ex.Message);
        }
    }

    public void Dispose()
    {
        _engine?.Dispose();
        _engine = null;
        _isInitialized = false;
        _initLock.Dispose();
    }

    private void InitializeEngine(RapidOcr engine)
    {
        if (_modelPaths is { IsConfigured: true } modelPaths)
        {
            ValidateModelPaths(modelPaths);
            engine.InitModels(
                modelPaths.DetectionModelPath,
                modelPaths.ClassificationModelPath,
                modelPaths.RecognitionModelPath,
                modelPaths.DictionaryPath,
                _intraOpThreads);
            return;
        }

        engine.InitModels(_intraOpThreads);
    }

    private static void ValidateModelPaths(OcrModelPaths modelPaths)
    {
        foreach (var file in modelPaths.AsEnumerable())
        {
            if (!File.Exists(file))
                throw new FileNotFoundException($"OCR model file was not found: {file}", file);
        }
    }

    private static double TryGetAverageConfidence(OcrResult result)
    {
        if (result.TextBlocks is null || result.TextBlocks.Length == 0)
            return 0;

        var scores = result.TextBlocks
            .SelectMany(block => block.CharScores?.Select(static score => (double)score) ?? [])
            .Where(static score => !double.IsNaN(score))
            .ToArray();

        if (scores.Length > 0)
            return scores.Average();

        return result.TextBlocks.Average(block => (double)block.BoxScore);
    }
}

public sealed record OcrModelPaths(
    string DetectionModelPath,
    string ClassificationModelPath,
    string RecognitionModelPath,
    string DictionaryPath)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(DetectionModelPath) &&
        !string.IsNullOrWhiteSpace(ClassificationModelPath) &&
        !string.IsNullOrWhiteSpace(RecognitionModelPath) &&
        !string.IsNullOrWhiteSpace(DictionaryPath);

    public IEnumerable<string> AsEnumerable()
    {
        yield return DetectionModelPath;
        yield return ClassificationModelPath;
        yield return RecognitionModelPath;
        yield return DictionaryPath;
    }
}
