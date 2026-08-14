using Kinana.AssetManagement.Application.Assets;
using Kinana.AssetManagement.Domain.Entities;

namespace Kinana.AssetManagement.Infrastructure.Data;

public sealed class AssetRepository : IAssetRepository
{
    private readonly AssetManagementDbContext _context;

    public AssetRepository(AssetManagementDbContext context)
    {
        _context = context;
    }

    public IQueryable<Asset> Assets => _context.Assets;

    public IQueryable<Category> Categories => _context.Categories;

    public IQueryable<AssetType> AssetTypes => _context.AssetTypes;

    public IQueryable<Department> Departments => _context.Departments;

    public IQueryable<Employee> Employees => _context.Employees;

    public IQueryable<Location> Locations => _context.Locations;

    public IQueryable<AssetTransfer> AssetTransfers => _context.AssetTransfers;

    public async Task AddAsync(Asset asset, CancellationToken ct)
    {
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(ct);
    }

    public void AddTransfer(AssetTransfer transfer)
        => _context.AssetTransfers.Add(transfer);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await operation(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public void SetOriginalRowVersion(Asset asset, byte[]? rowVersion)
        => _context.Entry(asset).Property(a => a.RowVersion).OriginalValue = rowVersion;

    public async Task<int> SaveChangesAsync(CancellationToken ct)
        => await _context.SaveChangesAsync(ct);
}
