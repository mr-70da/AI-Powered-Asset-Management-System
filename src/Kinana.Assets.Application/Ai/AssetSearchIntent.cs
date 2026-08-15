namespace Kinana.AssetManagement.Application.Ai;

/// <summary>
/// The structured, read-only description of an asset question that the LLM is
/// asked to produce (R4.2). The AI pipeline never translates the user's words
/// into SQL or writes; it only resolves this intent into the existing
/// <see cref="Assets.SearchAssetsQuery"/> shape and executes a read.
/// </summary>
public sealed class AssetSearchIntent
{
    /// <summary>"assetSearch" | "value" | "answer"</summary>
    public string IntentType { get; set; } = "assetSearch";

    public string? SearchTerm { get; set; }

    public string? CategoryName { get; set; }

    public string? AssetTypeName { get; set; }

    public string? Status { get; set; }

    public string? DepartmentName { get; set; }

    public string? LocationName { get; set; }

    public string? AssignedEmployeeName { get; set; }

    public bool CountOnly { get; set; }

    /// <summary>Direct answer text for greetings / out-of-scope questions ("answer" intent).</summary>
    public string? Answer { get; set; }
}
