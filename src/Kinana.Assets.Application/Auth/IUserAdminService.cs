namespace Kinana.AssetManagement.Application.Auth;

public interface IUserAdminService
{
    Task<List<UserProfileResponse>> GetAllAsync(CancellationToken ct);

    Task<UserProfileResponse> GetByIdAsync(int id, CancellationToken ct);

    Task<UserProfileResponse> CreateAsync(CreateUserRequest request, CancellationToken ct);

    Task SetRoleAsync(int userId, string roleName, CancellationToken ct);

    Task SetStatusAsync(int userId, bool isDisabled, CancellationToken ct);
}
