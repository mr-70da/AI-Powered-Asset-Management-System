using Kinana.AssetManagement.Application.Auth;
using Kinana.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kinana.AssetManagement.Infrastructure.Data;

public sealed class AuthRepository : IAuthRepository
{
    private readonly AssetManagementDbContext _context;

    public AuthRepository(AssetManagementDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken ct)
        => await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserName == userName, ct);

    public async Task<AppUser?> FindByIdAsync(int id, CancellationToken ct)
        => await _context.AppUsers
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == id, ct);

    public async Task<List<AppUser>> GetAllUsersAsync(CancellationToken ct)
        => await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);

    public async Task<Role?> FindRoleByNameAsync(string roleName, CancellationToken ct)
        => await _context.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Name == roleName, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct)
    {
        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken ct)
        => await _context.RefreshTokens
            .Include(t => t.AppUser)
                .ThenInclude(u => u.Role)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task SaveChangesAsync(CancellationToken ct)
        => await _context.SaveChangesAsync(ct);
}
