using System.ComponentModel.DataAnnotations;

namespace Kinana.AssetManagement.Application.Auth;

public sealed record CreateUserRequest(
    [Required] string UserName,
    [Required] string DisplayName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string RoleName);
