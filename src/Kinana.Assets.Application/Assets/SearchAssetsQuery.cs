namespace Kinana.AssetManagement.Application.Assets;

public sealed class SearchAssetsQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public int? AssetTypeId { get; set; }

    public string? Status { get; set; }

    public int? DepartmentId { get; set; }

    public int? LocationId { get; set; }

    public int? AssignedEmployeeId { get; set; }

    public string? SortBy { get; set; } = "assetCode";

    public string? SortDirection { get; set; } = "asc";
}
