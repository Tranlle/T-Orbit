using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Tranbok.Tools.Plugin.Promptor.Models;

namespace Tranbok.Tools.Plugin.Promptor.Services;

internal sealed class PromptOptimizationService : IDisposable
{
    private readonly HttpClient _httpClient = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async IAsyncEnumerable<string> OptimizeStreamAsync(
        string rawInput,
        OptimizationStrategy strategy,
        PromptorConfig config,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var systemPrompt = StrategyPrompts.Get(strategy);
        var endpoint = GetEndpoint(config);
        var body = BuildRequestJson(systemPrompt, rawInput, config, stream: true);

        var response = await SendAsync(endpoint, body, config.ApiKey, ct);
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var data = line["data: ".Length..];
            if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                break;

            if (!TryParseSseData(data, out var chunkRoot))
                continue;

            if (!chunkRoot.TryGetProperty("choices", out var choices))
                continue;

            if (choices.GetArrayLength() == 0) continue;

            if (!choices[0].TryGetProperty("delta", out var delta))
                continue;

            if (delta.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String)
            {
                var chunk = content.GetString();
                if (!string.IsNullOrEmpty(chunk))
                    yield return chunk;
            }
        }
    }

    public async Task<string> OptimizeAsync(
        string rawInput,
        OptimizationStrategy strategy,
        PromptorConfig config,
        CancellationToken ct = default)
    {
        var systemPrompt = StrategyPrompts.Get(strategy);
        var endpoint = GetEndpoint(config);
        var body = BuildRequestJson(systemPrompt, rawInput, config, stream: false);

        var response = await SendAsync(endpoint, body, config.ApiKey, ct);
        var rawJson = await response.Content.ReadAsStringAsync(ct);
        return ExtractContent(rawJson);
    }

    private async Task<HttpResponseMessage> SendAsync(
        string endpoint,
        string body,
        string apiKey,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (!string.IsNullOrEmpty(apiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(CancellationToken.None);
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}",
                null, response.StatusCode);
        }
        return response;
    }

    private static string BuildRequestJson(
        string systemPrompt,
        string userMessage,
        PromptorConfig config,
        bool stream)
    {
        var obj = new
        {
            model = config.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            max_tokens = config.MaxTokens,
            temperature = config.Temperature,
            stream
        };
        return JsonSerializer.Serialize(obj, JsonOpts);
    }

    private static string GetEndpoint(PromptorConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.ApiEndpoint))
        {
            var ep = config.ApiEndpoint.TrimEnd('/');
            if (!ep.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                ep += "/chat/completions";
            return ep;
        }

        return config.Provider.ToLowerInvariant() switch
        {
            "openai" => "https://api.openai.com/v1/chat/completions",
            "qwen" => "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
            "kimi" => "https://api.moonshot.cn/v1/chat/completions",
            "ollama" => "http://localhost:11434/v1/chat/completions",
            _ => throw new InvalidOperationException(
                $"未知提供商 '{config.Provider}'，请在插件变量中配置 PROMPTOR_API_ENDPOINT。")
        };
    }

    private static string ExtractContent(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                return string.Empty;
            var choice = choices[0];
            if (!choice.TryGetProperty("message", out var message))
                return string.Empty;
            if (!message.TryGetProperty("content", out var content))
                return string.Empty;
            return content.ValueKind == JsonValueKind.String
                ? content.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static bool TryParseSseData(string data, out JsonElement root)
    {
        root = default;
        try
        {
            root = JsonDocument.Parse(data).RootElement;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
