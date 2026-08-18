namespace Kinana.AssetManagement.Application.Ai;

public interface IAiService
{
    Task<AiChatResponse> AskAsync(AiChatRequest request, bool includeCost, CancellationToken ct);
}
