using Kinana.AssetManagement.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kinana.AssetManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;

    public UsersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserProfileResponse>>> GetAll(CancellationToken ct)
        => Ok(await _userAdminService.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserProfileResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _userAdminService.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<UserProfileResponse>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await _userAdminService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> SetRole(int id, SetUserRoleRequest request, CancellationToken ct)
    {
        await _userAdminService.SetRoleAsync(id, request.RoleName, ct);
        return NoContent();
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, SetUserStatusRequest request, CancellationToken ct)
    {
        await _userAdminService.SetStatusAsync(id, request.IsDisabled, ct);
        return NoContent();
    }
}
