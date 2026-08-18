using Kinana.AssetManagement.Domain.Entities;

namespace Kinana.AssetManagement.Application.Assets;

/// <summary>
/// The read-only surface of the asset store. This is the only repository
/// interface the AI pipeline is allowed to depend on: it exposes queryables
/// for reading but has no Add / Save / Transaction members, which makes the
/// AI path structurally incapable of writing to the database (R4.1).
/// </summary>
public interface IAssetReadRepository
{
    IQueryable<Asset> Assets { get; }

    IQueryable<Category> Categories { get; }

    IQueryable<AssetType> AssetTypes { get; }

    IQueryable<Department> Departments { get; }

    IQueryable<Employee> Employees { get; }

    IQueryable<Location> Locations { get; }
}
