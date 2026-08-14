using Kinana.AssetManagement.Domain.Entities;

namespace Kinana.AssetManagement.Application.Assets;

public interface IAssetRepository
{
    IQueryable<Asset> Assets { get; }

    IQueryable<Category> Categories { get; }

    IQueryable<AssetType> AssetTypes { get; }

    IQueryable<Department> Departments { get; }

    IQueryable<Employee> Employees { get; }

    IQueryable<Location> Locations { get; }

    IQueryable<AssetTransfer> AssetTransfers { get; }

    Task AddAsync(Asset asset, CancellationToken ct);

    void AddTransfer(AssetTransfer transfer);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct);

    void SetOriginalRowVersion(Asset asset, byte[]? rowVersion);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
