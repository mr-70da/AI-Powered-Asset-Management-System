using Kinana.AssetManagement.Application.Exceptions;
using Kinana.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kinana.AssetManagement.Application.Assets;

public interface IAssetService
{
    Task<AssetListResponse> ListAsync(SearchAssetsQuery query, bool includeCost, CancellationToken ct);

    Task<AssetResponse> GetByIdAsync(int id, bool includeCost, CancellationToken ct);
}

public sealed class AssetService : IAssetService
{
    private readonly IAssetRepository _repository;

    public AssetService(IAssetRepository repository)
    {
        _repository = repository;
    }

    public async Task<AssetListResponse> ListAsync(SearchAssetsQuery query, bool includeCost, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filtered = ApplyFilters(_repository.Assets.AsNoTracking(), query);

        var items = await ApplySort(filtered, query.SortBy, query.SortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AssetResponse(
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
                Array.Empty<AssetTransferResponse>()))
            .ToListAsync(ct);

        var totalCount = await filtered.CountAsync(ct);

        return new AssetListResponse(items, totalCount, page, pageSize);
    }

    public async Task<AssetResponse> GetByIdAsync(int id, bool includeCost, CancellationToken ct)
    {
        var asset = await _repository.Assets
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.AssetType)
            .Include(a => a.Department)
            .Include(a => a.AssignedEmployee)
            .Include(a => a.Location)
            .Include(a => a.CreatedByUser)
            .Include(a => a.ModifiedByUser)
            .Include(a => a.AssetTransfers)
                .ThenInclude(t => t.FromEmployee)
            .Include(a => a.AssetTransfers)
                .ThenInclude(t => t.ToEmployee)
            .Include(a => a.AssetTransfers)
                .ThenInclude(t => t.FromDepartment)
            .Include(a => a.AssetTransfers)
                .ThenInclude(t => t.ToDepartment)
            .Include(a => a.AssetTransfers)
                .ThenInclude(t => t.FromLocation)
            .Include(a => a.AssetTransfers)
                .ThenInclude(t => t.ToLocation)
            .Include(a => a.AssetTransfers)
                .ThenInclude(t => t.TransferredByUser)
            .SingleOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"Asset {id} was not found.");

        return ToResponse(asset, includeCost);
    }

    private static IQueryable<Asset> ApplyFilters(IQueryable<Asset> query, SearchAssetsQuery q)
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

    private static IOrderedQueryable<Asset> ApplySort(IQueryable<Asset> query, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy ?? string.Empty).ToLowerInvariant() switch
        {
            "assetname" => descending
                ? query.OrderByDescending(a => a.AssetName)
                : query.OrderBy(a => a.AssetName),
            "status" => descending
                ? query.OrderByDescending(a => a.Status)
                : query.OrderBy(a => a.Status),
            "category" => descending
                ? query.OrderByDescending(a => a.Category.Name)
                : query.OrderBy(a => a.Category.Name),
            "department" => descending
                ? query.OrderByDescending(a => a.Department!.Name)
                : query.OrderBy(a => a.Department!.Name),
            "location" => descending
                ? query.OrderByDescending(a => a.Location!.Name)
                : query.OrderBy(a => a.Location!.Name),
            "purchasedate" => descending
                ? query.OrderByDescending(a => a.PurchaseDate)
                : query.OrderBy(a => a.PurchaseDate),
            "createdat" => descending
                ? query.OrderByDescending(a => a.CreatedAtUtc)
                : query.OrderBy(a => a.CreatedAtUtc),
            _ => descending
                ? query.OrderByDescending(a => a.AssetCode)
                : query.OrderBy(a => a.AssetCode)
        };
    }

    private static AssetResponse ToResponse(Asset asset, bool includeCost)
    {
        var transfers = asset.AssetTransfers
            .OrderBy(t => t.TransferDateUtc)
            .Select(t => new AssetTransferResponse(
                t.Id,
                t.TransferDateUtc,
                t.Reason,
                t.FromEmployee?.Name,
                t.ToEmployee?.Name,
                t.FromDepartment?.Name,
                t.ToDepartment?.Name,
                t.FromLocation?.Name,
                t.ToLocation?.Name,
                t.TransferredByUser?.UserName ?? string.Empty))
            .ToList();

        return new AssetResponse(
            asset.Id,
            asset.AssetCode,
            asset.AssetName,
            asset.Description,
            asset.CategoryId,
            asset.Category.Name,
            asset.AssetTypeId,
            asset.AssetType.Name,
            asset.Manufacturer,
            asset.Model,
            asset.SerialNumber,
            asset.PurchaseDate,
            includeCost ? asset.PurchaseCost : null,
            asset.WarrantyExpiryDate,
            asset.Status,
            asset.DepartmentId,
            asset.Department?.Name,
            asset.AssignedEmployeeId,
            asset.AssignedEmployee?.Name,
            asset.LocationId,
            asset.Location?.Name,
            asset.CreatedByUserId,
            asset.CreatedByUser?.UserName,
            asset.CreatedAtUtc,
            asset.ModifiedByUserId,
            asset.ModifiedByUser?.UserName,
            asset.ModifiedAtUtc,
            transfers);
    }
}
