using System.Text.Json;

namespace Kinana.AssetManagement.Application.Ai;

/// <summary>
/// Turns the provider's raw completion into an <see cref="AssetSearchIntent"/>.
/// Defensive about common provider quirks: markdown fences around the JSON and
/// optional surrounding prose are stripped before deserialization.
/// </summary>
internal static class AiIntentParser
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static AssetSearchIntent Parse(string completion)
    {
        var json = ExtractJson(completion);
        var intent = JsonSerializer.Deserialize<AssetSearchIntent>(json, Options)
            ?? throw new InvalidOperationException("AI provider returned an empty intent.");

        return intent;
    }

    private static string ExtractJson(string completion)
    {
        var trimmed = completion.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = trimmed.IndexOf('\n');
            if (firstLineEnd > 0)
            {
                trimmed = trimmed[(firstLineEnd + 1)..];
            }
        }

        if (trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            trimmed = trimmed[..lastFence];
        }

        return trimmed.Trim();
    }
}
