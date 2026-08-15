using Kinana.AssetManagement.Domain.Entities;

namespace Kinana.AssetManagement.Application.Assets;

/// <summary>
/// Shared, read-only query composition used by both the asset list endpoint
/// and the AI assistant, so a question is executed by exactly the same filter
/// and projection logic as a manual search (R4.2: "your existing repository
/// executes"). No EF write method lives here.
/// </summary>
internal static class AssetQueries
{
    public static IQueryable<Asset> ApplyFilters(IQueryable<Asset> query, SearchAssetsQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            query = query.Where(a =>
                a.AssetCode.Contains(term)
                || a.AssetName.Contains(term)
                || (a.SerialNumber != null && a.SerialNumber.Contains(term)));
        }

        if (q.CategoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == q.CategoryId.Value);
        }

        if (q.AssetTypeId.HasValue)
        {
            query = query.Where(a => a.AssetTypeId == q.AssetTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            query = query.Where(a => a.Status == q.Status);
        }

        if (q.DepartmentId.HasValue)
        {
            query = query.Where(a => a.DepartmentId == q.DepartmentId.Value);
        }

        if (q.LocationId.HasValue)
        {
            query = query.Where(a => a.LocationId == q.LocationId.Value);
        }

        if (q.AssignedEmployeeId.HasValue)
        {
            query = query.Where(a => a.AssignedEmployeeId == q.AssignedEmployeeId.Value);
        }

        return query;
    }

    public static IQueryable<AssetResponse> Project(IQueryable<Asset> query, bool includeCost)
        => query.Select(a => new AssetResponse(
            a.Id,
            a.AssetCode,
            a.AssetName,
            a.Description,
            a.CategoryId,
            a.Category.Name,
            a.AssetTypeId,
            a.AssetType.Name,
            a.Manufacturer,
            a.Model,
            a.SerialNumber,
            a.PurchaseDate,
            includeCost ? a.PurchaseCost : null,
            a.WarrantyExpiryDate,
            a.Status,
            a.DepartmentId,
            a.Department != null ? a.Department.Name : null,
            a.AssignedEmployeeId,
            a.AssignedEmployee != null ? a.AssignedEmployee.Name : null,
            a.LocationId,
            a.Location != null ? a.Location.Name : null,
            a.CreatedByUserId,
            a.CreatedByUser != null ? a.CreatedByUser.UserName : null,
            a.CreatedAtUtc,
            a.ModifiedByUserId,
            a.ModifiedByUser != null ? a.ModifiedByUser.UserName : null,
            a.ModifiedAtUtc,
            a.RowVersion,
            Array.Empty<AssetTransferResponse>()));
}
