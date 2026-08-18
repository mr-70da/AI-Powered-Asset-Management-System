namespace Kinana.AssetManagement.Application.Lookups;

public interface ILookupService
{
    Task<LookupsResponse> GetLookupsAsync(CancellationToken ct);
}
