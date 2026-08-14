using System.ComponentModel.DataAnnotations;

namespace Kinana.AssetManagement.Application.Auth;

public sealed record LoginRequest(
    [Required] string UserName,
    [Required] string Password);
