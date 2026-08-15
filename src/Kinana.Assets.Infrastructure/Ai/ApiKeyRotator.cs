using Kinana.AssetManagement.Application.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kinana.AssetManagement.Infrastructure.Ai;

/// <summary>
/// Thread-safe singleton that cycles through the API keys in
/// <c>Ai:KeyFilePath</c> (one per line, '#' comments and blank lines ignored).
/// When a caller reports a 429 against the current key, the index advances so
/// the next request uses a fresh key. The file is gitignored — keys are never
/// committed (R4.6).
/// </summary>
public sealed class ApiKeyRotator : IApiKeyRotator
{
    private readonly ILogger<ApiKeyRotator> _logger;
    private readonly string[] _keys;
    private int _currentIndex;
    private readonly object _lock = new();

    public ApiKeyRotator(IOptions<AiSettings> settings, ILogger<ApiKeyRotator> logger)
    {
        _logger = logger;
        var keyFilePath = settings.Value.KeyFilePath;

        if (string.IsNullOrWhiteSpace(keyFilePath) || !File.Exists(keyFilePath))
        {
            _logger.LogWarning(
                "API key file '{KeyFilePath}' was not found. Key rotation is unavailable; " +
                "the provider will fall back to the single Ai:ApiKey value if configured.",
                keyFilePath);
            _keys = [];
            return;
        }

        _keys = File.ReadAllLines(keyFilePath)
            .Where(k => !string.IsNullOrWhiteSpace(k) && !k.TrimStart().StartsWith('#'))
            .Select(k => k.Trim())
            .ToArray();

        if (_keys.Length == 0)
        {
            _logger.LogWarning("API key file '{KeyFilePath}' contained no keys.", keyFilePath);
        }
    }

    public string? GetCurrentKey()
    {
        lock (_lock)
        {
            return _keys.Length == 0 ? null : _keys[_currentIndex];
        }
    }

    public void ReportKeyLimitReached(string failedKey)
    {
        lock (_lock)
        {
            // Only advance if the failing key is still the current one; this
            // prevents skipping keys when several threads fail at once.
            if (_keys.Length > 0 && _keys[_currentIndex] == failedKey)
            {
                _currentIndex = (_currentIndex + 1) % _keys.Length;
            }
        }
    }
}
