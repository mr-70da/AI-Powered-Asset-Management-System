namespace Kinana.AssetManagement.Application.Ai;

public sealed class AiSettings
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "stub";

    public string Endpoint { get; set; } = "https://api.openai.com/v1";

    public string Model { get; set; } = "gpt-4o-mini";

    public string? ApiKey { get; set; }

    public string KeyFilePath { get; set; } = "api_keys.txt";

    public int TimeoutSeconds { get; set; } = 20;

    public int MaxRows { get; set; } = 50;

    public int MaxRequestsPerMinutePerUser { get; set; } = 5;
}
