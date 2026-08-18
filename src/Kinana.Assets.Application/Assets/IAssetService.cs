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
