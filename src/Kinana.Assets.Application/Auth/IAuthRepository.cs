using Kinana.AssetManagement.Domain.Entities;

namespace Kinana.AssetManagement.Application.Auth;

public interface IAuthRepository
{
    Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken ct);

    Task<AppUser?> FindByIdAsync(int id, CancellationToken ct);

    Task<List<AppUser>> GetAllUsersAsync(CancellationToken ct);

    Task<Role?> FindRoleByNameAsync(string roleName, CancellationToken ct);

    Task AddAsync(AppUser user, CancellationToken ct);

    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct);

    Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
