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

    Task AddAsync(Asset asset, CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
