using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kinana.AssetManagement.Application.Ai;
using Kinana.AssetManagement.Application.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kinana.AssetManagement.Infrastructure.Ai;

/// <summary>
/// OpenAI-compatible chat-completions client. Handles key rotation on HTTP 429
/// via <see cref="IApiKeyRotator"/> and maps provider failures to a clear
/// <see cref="ServiceUnavailableException"/> so no raw provider error leaks to
/// the client (R4.5, R4.6).
/// </summary>
public sealed class OpenAiCompatibleProvider : IAiProvider
{
    private const int MaxAttempts = 4;

    private readonly HttpClient _httpClient;
    private readonly IApiKeyRotator _keyRotator;
    private readonly AiSettings _settings;
    private readonly ILogger<OpenAiCompatibleProvider> _logger;

    public OpenAiCompatibleProvider(
        HttpClient httpClient,
        IApiKeyRotator keyRotator,
        IOptions<AiSettings> settings,
        ILogger<OpenAiCompatibleProvider> logger)
    {
        _httpClient = httpClient;
        _keyRotator = keyRotator;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Name => "openai-compatible";

    public async Task<string> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var apiKey = ResolveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ServiceUnavailableException(
                    "The AI provider is not configured. Set Ai:ApiKey (user secrets or environment variable) " +
                    "or populate the api_keys.txt file.");
            }

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_settings.Endpoint.TrimEnd('/')}/chat/completions");

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = JsonContent.Create(new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                temperature = 0.2
            });

            try
            {
                using var response = await _httpClient.SendAsync(httpRequest, ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    using var document = JsonDocument.Parse(json);
                    var content = document.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    return content ?? string.Empty;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _keyRotator.ReportKeyLimitReached(apiKey);
                    _logger.LogWarning(
                        "AI provider returned 429; rotated to the next key and retrying (attempt {Attempt}/{MaxAttempts}).",
                        attempt + 1,
                        MaxAttempts);
                    await Task.Delay(500 * (attempt + 1), ct);
                    continue;
                }

                _logger.LogError("AI provider returned {StatusCode}.", response.StatusCode);
                throw new ServiceUnavailableException("The AI provider returned an error.");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new ServiceUnavailableException("The AI provider took too long to respond.");
            }
        }

        throw new ServiceUnavailableException("All configured AI provider keys are rate-limited or exhausted.");
    }

    private string? ResolveApiKey()
    {
        var rotated = _keyRotator.GetCurrentKey();
        if (!string.IsNullOrWhiteSpace(rotated))
        {
            return rotated;
        }

        return string.IsNullOrWhiteSpace(_settings.ApiKey) ? null : _settings.ApiKey;
    }
}
