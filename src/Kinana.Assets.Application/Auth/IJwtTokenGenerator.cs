namespace Kinana.AssetManagement.Application.Auth;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(JwtUserIdentity user);
}
