using System.Security.Cryptography;
using Kinana.AssetManagement.Application.Exceptions;
using Kinana.AssetManagement.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Kinana.AssetManagement.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IAuthRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtSettings> jwtSettings)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _repository.FindByUserNameAsync(request.UserName, ct);

        if (user is null
            || user.IsDisabled
            || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var stored = await _repository.FindRefreshTokenAsync(HashRefreshToken(request.RefreshToken), ct);

        if (stored is null || stored.RevokedAtUtc is not null || stored.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        if (stored.AppUser.IsDisabled)
        {
            throw new UnauthorizedException("Account is disabled.");
        }

        stored.RevokedAtUtc = DateTime.UtcNow;
        await _repository.SaveChangesAsync(ct);

        return await IssueTokensAsync(stored.AppUser, ct);
    }

    public async Task<UserProfileResponse> GetProfileAsync(int userId, CancellationToken ct)
    {
        var user = await _repository.FindByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        return ToProfile(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, CancellationToken ct)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(
            new JwtUserIdentity(user.Id, user.UserName, user.Role.Name));

        var (rawRefreshToken, refreshTokenHash) = CreateRefreshToken();

        var entity = new RefreshToken
        {
            AppUserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddRefreshTokenAsync(entity, ct);

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes),
            user.Role.Name);
    }

    private static (string Raw, string Hash) CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (raw, HashRefreshToken(raw));
    }

    private static string HashRefreshToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Convert.FromBase64String(token)));

    private static UserProfileResponse ToProfile(AppUser user)
        => new(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Email,
            user.Role.Name,
            user.IsDisabled,
            user.CreatedAtUtc);
}
