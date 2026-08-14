using Kinana.AssetManagement.Application.Common;
using Kinana.AssetManagement.Application.Exceptions;
using Kinana.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kinana.AssetManagement.Application.Assets;

public interface IAssetService
{
    Task<AssetListResponse> ListAsync(SearchAssetsQuery query, bool includeCost, CancellationToken ct);

    Task<AssetResponse> GetByIdAsync(int id, bool includeCost, CancellationToken ct);

    Task<AssetResponse> CreateAsync(CreateAssetRequest request, CancellationToken ct);

    Task<AssetResponse> UpdateAsync(int id, UpdateAssetRequest request, CancellationToken ct);

    Task RetireAsync(int id, CancellationToken ct);

    Task TransferAsync(int id, TransferAssetRequest request, CancellationToken ct);

    Task<IReadOnlyList<AssetTransferResponse>> GetTransfersAsync(int id, CancellationToken ct);
}

public sealed class AssetService : IAssetService
{
    private readonly IAssetRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public AssetService(IAssetRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
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

    public async Task<AssetResponse> CreateAsync(CreateAssetRequest request, CancellationToken ct)
    {
        await ValidateReferencesAsync(
            request.CategoryId,
            request.AssetTypeId,
            request.DepartmentId,
            request.AssignedEmployeeId,
            request.LocationId,
            ct);

        if (await _repository.Assets.AnyAsync(a => a.AssetCode == request.AssetCode, ct))
        {
            throw new ConflictException($"An asset with code '{request.AssetCode}' already exists.");
        }

        if (request.SerialNumber is not null
            && await _repository.Assets.AnyAsync(a => a.SerialNumber == request.SerialNumber, ct))
        {
            throw new ConflictException($"An asset with serial number '{request.SerialNumber}' already exists.");
        }

        var now = DateTime.UtcNow;
        var asset = new Asset
        {
            AssetCode = request.AssetCode,
            AssetName = request.AssetName,
            Description = request.Description,
            CategoryId = request.CategoryId,
            AssetTypeId = request.AssetTypeId,
            Manufacturer = request.Manufacturer,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            PurchaseDate = request.PurchaseDate,
            PurchaseCost = request.PurchaseCost,
            WarrantyExpiryDate = request.WarrantyExpiryDate,
            Status = request.Status,
            DepartmentId = request.DepartmentId,
            AssignedEmployeeId = request.AssignedEmployeeId,
            LocationId = request.LocationId,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = now,
            ModifiedByUserId = _currentUser.UserId,
            ModifiedAtUtc = now
        };

        await _repository.AddAsync(asset, ct);

        return await GetByIdAsync(asset.Id, includeCost: true, ct);
    }

    public async Task<AssetResponse> UpdateAsync(int id, UpdateAssetRequest request, CancellationToken ct)
    {
        var asset = await _repository.Assets.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"Asset {id} was not found.");

        await ValidateReferencesAsync(
            request.CategoryId,
            request.AssetTypeId,
            request.DepartmentId,
            request.AssignedEmployeeId,
            request.LocationId,
            ct);

        if (await _repository.Assets.AnyAsync(a => a.Id != id && a.AssetCode == request.AssetCode, ct))
        {
            throw new ConflictException($"An asset with code '{request.AssetCode}' already exists.");
        }

        if (request.SerialNumber is not null
            && await _repository.Assets.AnyAsync(a => a.Id != id && a.SerialNumber == request.SerialNumber, ct))
        {
            throw new ConflictException($"An asset with serial number '{request.SerialNumber}' already exists.");
        }

        asset.AssetCode = request.AssetCode;
        asset.AssetName = request.AssetName;
        asset.Description = request.Description;
        asset.CategoryId = request.CategoryId;
        asset.AssetTypeId = request.AssetTypeId;
        asset.Manufacturer = request.Manufacturer;
        asset.Model = request.Model;
        asset.SerialNumber = request.SerialNumber;
        asset.PurchaseDate = request.PurchaseDate;
        asset.PurchaseCost = request.PurchaseCost;
        asset.WarrantyExpiryDate = request.WarrantyExpiryDate;
        asset.Status = request.Status;
        asset.DepartmentId = request.DepartmentId;
        asset.AssignedEmployeeId = request.AssignedEmployeeId;
        asset.LocationId = request.LocationId;
        asset.ModifiedByUserId = _currentUser.UserId;
        asset.ModifiedAtUtc = DateTime.UtcNow;

        await _repository.SaveChangesAsync(ct);

        return await GetByIdAsync(id, includeCost: true, ct);
    }

    public async Task RetireAsync(int id, CancellationToken ct)
    {
        var asset = await _repository.Assets.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"Asset {id} was not found.");

        if (asset.Status == "Retired")
        {
            throw new ConflictException($"Asset {id} is already retired.");
        }

        asset.Status = "Retired";
        asset.ModifiedByUserId = _currentUser.UserId;
        asset.ModifiedAtUtc = DateTime.UtcNow;

        await _repository.SaveChangesAsync(ct);
    }

    public async Task TransferAsync(int id, TransferAssetRequest request, CancellationToken ct)
    {
        var asset = await _repository.Assets.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"Asset {id} was not found.");

        if (asset.Status == "Retired")
        {
            throw new ValidationException($"Asset {id} is retired and cannot be transferred.");
        }

        if (asset.DepartmentId == request.ToDepartmentId
            && asset.AssignedEmployeeId == request.ToEmployeeId
            && asset.LocationId == request.ToLocationId)
        {
            throw new ValidationException("A transfer must change the department, employee, or location.");
        }

        await ValidateTransferReferencesAsync(request, ct);

        _repository.AddTransfer(new AssetTransfer
        {
            AssetId = asset.Id,
            FromDepartmentId = asset.DepartmentId,
            ToDepartmentId = request.ToDepartmentId,
            FromEmployeeId = asset.AssignedEmployeeId,
            ToEmployeeId = request.ToEmployeeId,
            FromLocationId = asset.LocationId,
            ToLocationId = request.ToLocationId,
            TransferDateUtc = request.TransferDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Reason = request.Reason,
            TransferredByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow
        });

        asset.DepartmentId = request.ToDepartmentId;
        asset.AssignedEmployeeId = request.ToEmployeeId;
        asset.LocationId = request.ToLocationId;
        asset.ModifiedByUserId = _currentUser.UserId;
        asset.ModifiedAtUtc = DateTime.UtcNow;

        await _repository.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AssetTransferResponse>> GetTransfersAsync(int id, CancellationToken ct)
    {
        if (!await _repository.Assets.AnyAsync(a => a.Id == id, ct))
        {
            throw new NotFoundException($"Asset {id} was not found.");
        }

        return await _repository.AssetTransfers
            .AsNoTracking()
            .Include(t => t.FromEmployee)
            .Include(t => t.ToEmployee)
            .Include(t => t.FromDepartment)
            .Include(t => t.ToDepartment)
            .Include(t => t.FromLocation)
            .Include(t => t.ToLocation)
            .Include(t => t.TransferredByUser)
            .Where(t => t.AssetId == id)
            .OrderBy(t => t.TransferDateUtc)
            .ThenBy(t => t.Id)
            .Select(t => new AssetTransferResponse(
                t.Id,
                t.TransferDateUtc,
                t.Reason,
                t.FromEmployee != null ? t.FromEmployee.Name : null,
                t.ToEmployee != null ? t.ToEmployee.Name : null,
                t.FromDepartment != null ? t.FromDepartment.Name : null,
                t.ToDepartment != null ? t.ToDepartment.Name : null,
                t.FromLocation != null ? t.FromLocation.Name : null,
                t.ToLocation != null ? t.ToLocation.Name : null,
                t.TransferredByUser.UserName))
            .ToListAsync(ct);
    }

    private async Task ValidateReferencesAsync(
        int categoryId,
        int assetTypeId,
        int? departmentId,
        int? employeeId,
        int? locationId,
        CancellationToken ct)
    {
        if (!await _repository.Categories.AnyAsync(c => c.Id == categoryId, ct))
        {
            throw new ValidationException($"Category {categoryId} does not exist.");
        }

        if (!await _repository.AssetTypes.AnyAsync(t => t.Id == assetTypeId, ct))
        {
            throw new ValidationException($"Asset type {assetTypeId} does not exist.");
        }

        if (departmentId.HasValue && !await _repository.Departments.AnyAsync(d => d.Id == departmentId.Value, ct))
        {
            throw new ValidationException($"Department {departmentId} does not exist.");
        }

        if (employeeId.HasValue && !await _repository.Employees.AnyAsync(e => e.Id == employeeId.Value, ct))
        {
            throw new ValidationException($"Employee {employeeId} does not exist.");
        }

        if (locationId.HasValue && !await _repository.Locations.AnyAsync(l => l.Id == locationId.Value, ct))
        {
            throw new ValidationException($"Location {locationId} does not exist.");
        }
    }

    private async Task ValidateTransferReferencesAsync(TransferAssetRequest request, CancellationToken ct)
    {
        if (request.ToDepartmentId.HasValue
            && !await _repository.Departments.AnyAsync(d => d.Id == request.ToDepartmentId.Value, ct))
        {
            throw new ValidationException($"Department {request.ToDepartmentId} does not exist.");
        }

        if (request.ToEmployeeId.HasValue
            && !await _repository.Employees.AnyAsync(e => e.Id == request.ToEmployeeId.Value, ct))
        {
            throw new ValidationException($"Employee {request.ToEmployeeId} does not exist.");
        }

        if (request.ToLocationId.HasValue
            && !await _repository.Locations.AnyAsync(l => l.Id == request.ToLocationId.Value, ct))
        {
            throw new ValidationException($"Location {request.ToLocationId} does not exist.");
        }
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
