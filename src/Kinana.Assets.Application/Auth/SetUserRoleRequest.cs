using System.ComponentModel.DataAnnotations;

namespace Kinana.AssetManagement.Application.Auth;

public sealed record SetUserRoleRequest(
    [Required] string RoleName);
