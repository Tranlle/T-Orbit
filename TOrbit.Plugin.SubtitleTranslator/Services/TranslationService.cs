using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TOrbit.Plugin.SubtitleTranslator.Models;

namespace TOrbit.Plugin.SubtitleTranslator.Services;

public sealed class TranslationService
{
    private static readonly HttpClient HttpClient = new();
    private TranslationServiceOptions _options = TranslationServiceOptions.Disabled;

    public void Configure(TranslationServiceOptions options)
    {
        _options = options ?? TranslationServiceOptions.Disabled;
    }

    public async Task<TranslationResult> TranslateAsync(
        string sourceText,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceText);
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = DateTime.UtcNow;

        if (!_options.IsEnabled)
        {
            return new TranslationResult(
                SourceText: sourceText,
                TranslatedText: string.Empty,
                TargetLanguage: _options.TargetLanguage,
                Duration: DateTime.UtcNow - startedAt,
                IsSuccess: false,
                ErrorMessage: "Translation is not configured.");
        }

        if (string.Equals(_options.Provider, TranslationProviders.Echo, StringComparison.OrdinalIgnoreCase))
        {
            var translated = $"[{_options.TargetLanguage}] {sourceText}";
            return new TranslationResult(
                SourceText: sourceText,
                TranslatedText: translated,
                TargetLanguage: _options.TargetLanguage,
                Duration: DateTime.UtcNow - startedAt,
                IsSuccess: true);
        }

        if (string.Equals(_options.Provider, TranslationProviders.OpenAiCompatible, StringComparison.OrdinalIgnoreCase))
            return await TranslateWithOpenAiCompatibleAsync(sourceText, startedAt, cancellationToken);

        return new TranslationResult(
            SourceText: sourceText,
            TranslatedText: string.Empty,
            TargetLanguage: _options.TargetLanguage,
            Duration: DateTime.UtcNow - startedAt,
            IsSuccess: false,
            ErrorMessage: $"Provider '{_options.Provider}' is not implemented yet.");
    }

    private async Task<TranslationResult> TranslateWithOpenAiCompatibleAsync(
        string sourceText,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            return new TranslationResult(
                SourceText: sourceText,
                TranslatedText: string.Empty,
                TargetLanguage: _options.TargetLanguage,
                Duration: DateTime.UtcNow - startedAt,
                IsSuccess: false,
                ErrorMessage: "Translation endpoint is required for the openai-compatible provider.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            return new TranslationResult(
                SourceText: sourceText,
                TranslatedText: string.Empty,
                TargetLanguage: _options.TargetLanguage,
                Duration: DateTime.UtcNow - startedAt,
                IsSuccess: false,
                ErrorMessage: "Translation model is required for the openai-compatible provider.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        request.Content = JsonContent.Create(new OpenAiCompatibleRequest(
            Model: _options.Model,
            Messages:
            [
                new OpenAiCompatibleMessage(
                    Role: "developer",
                    Content: $"You translate subtitles into {_options.TargetLanguage}. Return only the translated text without explanations."),
                new OpenAiCompatibleMessage(
                    Role: "user",
                    Content: sourceText)
            ]));

        try
        {
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<OpenAiCompatibleResponse>(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = payload?.Error?.Message;
                return new TranslationResult(
                    SourceText: sourceText,
                    TranslatedText: string.Empty,
                    TargetLanguage: _options.TargetLanguage,
                    Duration: DateTime.UtcNow - startedAt,
                    IsSuccess: false,
                    ErrorMessage: string.IsNullOrWhiteSpace(message)
                        ? $"Translation request failed with status {(int)response.StatusCode}."
                        : message);
            }

            var translated = payload?.Choices?
                .FirstOrDefault()?
                .Message?
                .Content?
                .Trim();

            return new TranslationResult(
                SourceText: sourceText,
                TranslatedText: translated ?? string.Empty,
                TargetLanguage: _options.TargetLanguage,
                Duration: DateTime.UtcNow - startedAt,
                IsSuccess: !string.IsNullOrWhiteSpace(translated),
                ErrorMessage: string.IsNullOrWhiteSpace(translated)
                    ? "Translation response did not contain any text."
                    : null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TranslationResult(
                SourceText: sourceText,
                TranslatedText: string.Empty,
                TargetLanguage: _options.TargetLanguage,
                Duration: DateTime.UtcNow - startedAt,
                IsSuccess: false,
                ErrorMessage: ex.Message);
        }
    }
}

public sealed record TranslationServiceOptions(
    string Provider,
    string TargetLanguage,
    string? Model = null,
    string? Endpoint = null,
    string? ApiKey = null)
{
    public static TranslationServiceOptions Disabled { get; } = new(
        Provider: TranslationProviders.Disabled,
        TargetLanguage: "zh-CN");

    public bool IsEnabled => !string.Equals(Provider, TranslationProviders.Disabled, StringComparison.OrdinalIgnoreCase);
}

public static class TranslationProviders
{
    public const string Disabled = "disabled";
    public const string Echo = "echo";
    public const string OpenAiCompatible = "openai-compatible";
}

public sealed record OpenAiCompatibleRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<OpenAiCompatibleMessage> Messages);

public sealed record OpenAiCompatibleMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

public sealed record OpenAiCompatibleResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiCompatibleChoice>? Choices,
    [property: JsonPropertyName("error")] OpenAiCompatibleError? Error);

public sealed record OpenAiCompatibleChoice(
    [property: JsonPropertyName("message")] OpenAiCompatibleAssistantMessage? Message);

public sealed record OpenAiCompatibleAssistantMessage(
    [property: JsonPropertyName("content")] string? Content);

public sealed record OpenAiCompatibleError(
    [property: JsonPropertyName("message")] string? Message);
