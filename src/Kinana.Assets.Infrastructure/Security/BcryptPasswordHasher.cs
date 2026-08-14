using BCryptNet = BCrypt.Net.BCrypt;
using Kinana.AssetManagement.Application.Auth;

namespace Kinana.AssetManagement.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCryptNet.HashPassword(password);

    public bool Verify(string password, string passwordHash) => BCryptNet.Verify(password, passwordHash);
}
