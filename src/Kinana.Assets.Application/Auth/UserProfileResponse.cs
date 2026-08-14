namespace Kinana.AssetManagement.Application.Auth;

public sealed record UserProfileResponse(
    int Id,
    string UserName,
    string DisplayName,
    string Email,
    string Role,
    bool IsDisabled,
    DateTime CreatedAtUtc);
