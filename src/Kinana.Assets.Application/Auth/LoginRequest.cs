using System.ComponentModel.DataAnnotations;

namespace Kinana.AssetManagement.Application.Auth;

public sealed record LoginRequest(
    [property: Required] string UserName,
    [property: Required] string Password);
