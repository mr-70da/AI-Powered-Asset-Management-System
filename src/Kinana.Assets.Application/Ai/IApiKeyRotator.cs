namespace Kinana.AssetManagement.Application.Ai;

public interface IApiKeyRotator
{
    string? GetCurrentKey();

    void ReportKeyLimitReached(string failedKey);
}
