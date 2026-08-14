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

    public string AssetDetailPattern(int assetId)
        => $"{_globalPrefix}*:Asset_{assetId}";

    public string AssetListPattern()
        => $"{_globalPrefix}*:Assets_*";

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
