using System.ComponentModel.DataAnnotations;

namespace Kinana.AssetManagement.Application.Auth;

public sealed record RefreshRequest(
    [property: Required] string RefreshToken);
