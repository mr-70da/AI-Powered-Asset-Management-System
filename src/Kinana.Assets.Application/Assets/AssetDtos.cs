namespace Kinana.AssetManagement.Application.Assets;

public sealed record CreateAssetRequest
{
    public string AssetCode { get; init; } = string.Empty;

    public string AssetName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int CategoryId { get; init; }

    public int AssetTypeId { get; init; }

    public string Manufacturer { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public string? SerialNumber { get; init; }

    public DateOnly? PurchaseDate { get; init; }

    public decimal? PurchaseCost { get; init; }

    public DateOnly? WarrantyExpiryDate { get; init; }

    public string Status { get; init; } = "Available";

    public int? DepartmentId { get; init; }

    public int? AssignedEmployeeId { get; init; }

    public int? LocationId { get; init; }
}

public sealed record UpdateAssetRequest
{
    public string AssetCode { get; init; } = string.Empty;

    public string AssetName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int CategoryId { get; init; }

    public int AssetTypeId { get; init; }

    public string Manufacturer { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public string? SerialNumber { get; init; }

    public DateOnly? PurchaseDate { get; init; }

    public decimal? PurchaseCost { get; init; }

    public DateOnly? WarrantyExpiryDate { get; init; }

    public string Status { get; init; } = "Available";

    public int? DepartmentId { get; init; }

    public int? AssignedEmployeeId { get; init; }

    public int? LocationId { get; init; }
}

public sealed record AssetTransferResponse(
    int Id,
    DateTime TransferDateUtc,
    string? Reason,
    string? FromEmployeeName,
    string? ToEmployeeName,
    string? FromDepartmentName,
    string? ToDepartmentName,
    string? FromLocationName,
    string? ToLocationName,
    string TransferredByUserName);

public sealed record AssetResponse(
    int Id,
    string AssetCode,
    string AssetName,
    string? Description,
    int CategoryId,
    string CategoryName,
    int AssetTypeId,
    string AssetTypeName,
    string Manufacturer,
    string Model,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? PurchaseCost,
    DateOnly? WarrantyExpiryDate,
    string Status,
    int? DepartmentId,
    string? DepartmentName,
    int? AssignedEmployeeId,
    string? AssignedEmployeeName,
    int? LocationId,
    string? LocationName,
    int? CreatedByUserId,
    string? CreatedByUserName,
    DateTime CreatedAtUtc,
    int? ModifiedByUserId,
    string? ModifiedByUserName,
    DateTime? ModifiedAtUtc,
    IReadOnlyList<AssetTransferResponse> Transfers);

public sealed record AssetListResponse(
    IReadOnlyList<AssetResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
