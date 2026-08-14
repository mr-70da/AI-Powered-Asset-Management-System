using Kinana.AssetManagement.Application.Exceptions;
using Kinana.AssetManagement.Domain.Entities;

namespace Kinana.AssetManagement.Application.Auth;

public sealed class UserAdminService : IUserAdminService
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;

    public UserAdminService(IAuthRepository repository, IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserProfileResponse>> GetAllAsync(CancellationToken ct)
    {
        var users = await _repository.GetAllUsersAsync(ct);
        return users.Select(ToProfile).ToList();
    }

    public async Task<UserProfileResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var user = await _repository.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("User not found.");

        return ToProfile(user);
    }

    public async Task<UserProfileResponse> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var userName = request.UserName.Trim();
        if (await _repository.FindByUserNameAsync(userName, ct) is not null)
        {
            throw new ConflictException($"A user named '{userName}' already exists.");
        }

        var role = await FindRoleAsync(request.RoleName, ct);

        var user = new AppUser
        {
            UserName = userName,
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            RoleId = role.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(user, ct);

        var created = await _repository.FindByIdAsync(user.Id, ct)
            ?? throw new NotFoundException("User not found.");

        return ToProfile(created);
    }

    public async Task SetRoleAsync(int userId, string roleName, CancellationToken ct)
    {
        var user = await _repository.FindByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        var role = await FindRoleAsync(roleName, ct);

        user.RoleId = role.Id;
        user.Role = role;

        await _repository.SaveChangesAsync(ct);
    }

    public async Task SetStatusAsync(int userId, bool isDisabled, CancellationToken ct)
    {
        var user = await _repository.FindByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        user.IsDisabled = isDisabled;

        await _repository.SaveChangesAsync(ct);
    }

    private async Task<Role> FindRoleAsync(string roleName, CancellationToken ct)
    {
        var normalized = roleName.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "user" => "User",
            _ => roleName.Trim()
        };

        return await _repository.FindRoleByNameAsync(normalized, ct)
            ?? throw new ConflictException($"Role '{normalized}' does not exist.");
    }

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
