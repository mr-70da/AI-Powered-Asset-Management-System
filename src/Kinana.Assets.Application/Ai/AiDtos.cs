using Kinana.AssetManagement.Application.Assets;

namespace Kinana.AssetManagement.Application.Ai;

public sealed record AiChatRequest
{
    public string Question { get; init; } = string.Empty;
}

public sealed record AiChatResponse(
    string Answer,
    IReadOnlyList<AssetResponse> Rows,
    int TotalCount);
