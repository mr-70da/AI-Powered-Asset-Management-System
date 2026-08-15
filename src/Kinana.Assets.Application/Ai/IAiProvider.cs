namespace Kinana.AssetManagement.Application.Ai;

public sealed record AiCompletionRequest(string SystemPrompt, string UserPrompt);

/// <summary>
/// Owned abstraction over the external LLM provider so it can be swapped or
/// faked in tests (R4.6, section 04). Implementations translate the prompt
/// pair into a completion and return the raw text.
/// </summary>
public interface IAiProvider
{
    string Name { get; }

    Task<string> CompleteAsync(AiCompletionRequest request, CancellationToken ct);
}
