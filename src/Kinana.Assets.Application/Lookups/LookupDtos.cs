namespace Kinana.AssetManagement.Application.Lookups;

public sealed record LookupItemDto(int Id, string Name);

public sealed record LookupsResponse(
    IReadOnlyList<LookupItemDto> Categories,
    IReadOnlyList<LookupItemDto> AssetTypes,
    IReadOnlyList<LookupItemDto> Departments,
    IReadOnlyList<LookupItemDto> Locations,
    IReadOnlyList<LookupItemDto> Employees);
