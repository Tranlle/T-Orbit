using TOrbit.Plugin.SubtitleTranslator.Models;

namespace TOrbit.Plugin.SubtitleTranslator.Services;

public sealed class SubtitleStabilizerService
{
    private string _lastAcceptedText = string.Empty;
    private DateTimeOffset _lastAcceptedAt = DateTimeOffset.MinValue;

    public SubtitleFrameState Process(
        string? rawText,
        DateTimeOffset timestamp,
        SubtitleStabilizerOptions? options = null)
    {
        options ??= SubtitleStabilizerOptions.Default;

        var cleaned = Normalize(rawText);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return new SubtitleFrameState(
                StableText: string.Empty,
                IsNewSubtitle: false,
                Similarity: 1,
                ShouldTranslate: false,
                UpdatedAt: timestamp);
        }

        if (string.IsNullOrWhiteSpace(_lastAcceptedText))
        {
            _lastAcceptedText = cleaned;
            _lastAcceptedAt = timestamp;
            return new SubtitleFrameState(
                StableText: cleaned,
                IsNewSubtitle: true,
                Similarity: 0,
                ShouldTranslate: true,
                UpdatedAt: timestamp);
        }

        var similarity = CalculateSimilarity(_lastAcceptedText, cleaned);
        var elapsed = timestamp - _lastAcceptedAt;
        var isNearDuplicate = similarity >= options.SimilarityThreshold;

        if (isNearDuplicate && elapsed < options.MinRefreshInterval)
        {
            return new SubtitleFrameState(
                StableText: _lastAcceptedText,
                IsNewSubtitle: false,
                Similarity: similarity,
                ShouldTranslate: false,
                UpdatedAt: _lastAcceptedAt);
        }

        if (IsIncrementalExtension(_lastAcceptedText, cleaned) && elapsed < options.StabilizationWindow)
        {
            _lastAcceptedText = cleaned;
            _lastAcceptedAt = timestamp;
            return new SubtitleFrameState(
                StableText: cleaned,
                IsNewSubtitle: true,
                Similarity: similarity,
                ShouldTranslate: true,
                UpdatedAt: timestamp);
        }

        if (!isNearDuplicate)
        {
            _lastAcceptedText = cleaned;
            _lastAcceptedAt = timestamp;
            return new SubtitleFrameState(
                StableText: cleaned,
                IsNewSubtitle: true,
                Similarity: similarity,
                ShouldTranslate: true,
                UpdatedAt: timestamp);
        }

        return new SubtitleFrameState(
            StableText: _lastAcceptedText,
            IsNewSubtitle: false,
            Similarity: similarity,
            ShouldTranslate: false,
            UpdatedAt: _lastAcceptedAt);
    }

    public void Reset()
    {
        _lastAcceptedText = string.Empty;
        _lastAcceptedAt = DateTimeOffset.MinValue;
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);

        return normalized;
    }

    private static bool IsIncrementalExtension(string previous, string current)
        => current.Length > previous.Length &&
           current.StartsWith(previous, StringComparison.OrdinalIgnoreCase);

    private static double CalculateSimilarity(string left, string right)
    {
        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            return 1;

        var distance = LevenshteinDistance(left, right);
        var maxLength = Math.Max(left.Length, right.Length);
        if (maxLength == 0)
            return 1;

        return 1d - ((double)distance / maxLength);
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var rows = left.Length + 1;
        var cols = right.Length + 1;
        var matrix = new int[rows, cols];

        for (var i = 0; i < rows; i++)
            matrix[i, 0] = i;

        for (var j = 0; j < cols; j++)
            matrix[0, j] = j;

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < cols; j++)
            {
                var cost = char.ToUpperInvariant(left[i - 1]) == char.ToUpperInvariant(right[j - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[rows - 1, cols - 1];
    }
}

public sealed record SubtitleStabilizerOptions(
    double SimilarityThreshold,
    TimeSpan MinRefreshInterval,
    TimeSpan StabilizationWindow)
{
    public static SubtitleStabilizerOptions Default { get; } = new(
        SimilarityThreshold: 0.92,
        MinRefreshInterval: TimeSpan.FromMilliseconds(600),
        StabilizationWindow: TimeSpan.FromMilliseconds(900));
}
