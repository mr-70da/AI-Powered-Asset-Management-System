using FluentValidation;
using Kinana.AssetManagement.Application.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kinana.AssetManagement.Api.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly IValidator<CreateAssetRequest> _createValidator;
    private readonly IValidator<UpdateAssetRequest> _updateValidator;

    public AssetsController(
        IAssetService assetService,
        IValidator<CreateAssetRequest> createValidator,
        IValidator<UpdateAssetRequest> updateValidator)
    {
        _assetService = assetService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<AssetListResponse>> GetList(
        [FromQuery] SearchAssetsQuery query,
        CancellationToken ct)
        => Ok(await _assetService.ListAsync(query, includeCost: User.IsInRole("Admin"), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _assetService.GetByIdAsync(id, includeCost: User.IsInRole("Admin"), ct));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AssetResponse>> Create(CreateAssetRequest request, CancellationToken ct)
    {
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            AddModelErrors(validationResult);
            return ValidationProblem(ModelState);
        }

        var asset = await _assetService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetResponse>> Update(int id, UpdateAssetRequest request, CancellationToken ct)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            AddModelErrors(validationResult);
            return ValidationProblem(ModelState);
        }

        var asset = await _assetService.UpdateAsync(id, request, ct);
        return Ok(asset);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/retire")]
    public async Task<IActionResult> Retire(int id, CancellationToken ct)
    {
        await _assetService.RetireAsync(id, ct);
        return NoContent();
    }

    private void AddModelErrors(FluentValidation.Results.ValidationResult validationResult)
    {
        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
