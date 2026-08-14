using System.Security.Claims;
using Kinana.AssetManagement.Application.Common;
using Kinana.AssetManagement.Application.Exceptions;

namespace Kinana.AssetManagement.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var userId)
                ? userId
                : throw new UnauthorizedException("Unable to determine the current user.");
        }
    }
}
