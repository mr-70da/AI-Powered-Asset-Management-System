using Kinana.AssetManagement.Application.Lookups;
using Microsoft.AspNetCore.Mvc;

namespace Kinana.AssetManagement.Api.Controllers;

[ApiController]
[Route("api/lookups")]
public sealed class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<ActionResult<LookupsResponse>> Get(CancellationToken ct)
        => Ok(await _lookupService.GetLookupsAsync(ct));
}
