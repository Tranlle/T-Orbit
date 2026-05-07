namespace TOrbit.Plugin.SubtitleTranslator.Models;

public sealed record OcrFrameResult(
    string RawText,
    double Confidence,
    TimeSpan Duration,
    DateTimeOffset CapturedAt,
    bool IsSuccess,
    string? ErrorMessage = null);
