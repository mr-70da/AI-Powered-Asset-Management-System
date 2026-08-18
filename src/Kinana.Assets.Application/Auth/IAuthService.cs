namespace Kinana.AssetManagement.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);

    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct);

    Task<UserProfileResponse> GetProfileAsync(int userId, CancellationToken ct);
}
