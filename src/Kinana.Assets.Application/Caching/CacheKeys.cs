using System.Security.Cryptography;
using System.Text;
using Kinana.AssetManagement.Application.Assets;
using Microsoft.Extensions.Options;

namespace Kinana.AssetManagement.Application.Caching;

public sealed class CacheKeys
{
    private readonly string _globalPrefix;

    public CacheKeys(IOptions<CacheSettings> settings)
    {
        _globalPrefix = settings.Value.GlobalPrefix;
    }

    public string AssetDetail(int assetId, string role)
        => $"{_globalPrefix}{role}:Asset_{assetId}";

    public string AssetList(string role, SearchAssetsQuery query)
        => $"{_globalPrefix}{role}:{BuildAssetListKey(query)}";

    public string Lookups()
        => $"{_globalPrefix}Lookups:All";

    /// <summary>
    /// AI answers are keyed by normalized question plus role, so a User never
    /// reads an Admin-cached answer (R5.4) and equivalent questions share one
    /// entry (R5.8). The hash keeps the key compact and free of user text.
    /// </summary>
    public string AiAnswer(string role, string question)
    {
        var normalized = Normalize(question);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))[..16].ToLowerInvariant();
        return $"{_globalPrefix}{role}:Ai:{hash}";
    }

    public string AssetDetailPattern(int assetId)
        => $"{_globalPrefix}*:Asset_{assetId}";

    public string AssetListPattern()
        => $"{_globalPrefix}*:Assets_*";

    public string AiAnswerPattern()
        => $"{_globalPrefix}*:Ai:*";

    private static string Normalize(string value)
        => string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static string BuildAssetListKey(SearchAssetsQuery q)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var sortBy = (q.SortBy ?? "assetCode").Trim().ToLowerInvariant();
        var sortDirection = (q.SortDirection ?? "asc").Trim().ToLowerInvariant();
        var search = (q.Search ?? string.Empty).Trim();
        var status = (q.Status ?? string.Empty).Trim();

        return string.Join(
            "_",
            "Assets",
            $"Page{page}",
            $"Size{pageSize}",
            $"Search{search}",
            $"Category{q.CategoryId ?? 0}",
            $"Type{q.AssetTypeId ?? 0}",
            $"Status{status}",
            $"Department{q.DepartmentId ?? 0}",
            $"Location{q.LocationId ?? 0}",
            $"Employee{q.AssignedEmployeeId ?? 0}",
            $"Sort{sortBy}:{sortDirection}");
    }
}
