using FluentValidation;
using Kinana.AssetManagement.Application.Ai;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kinana.AssetManagement.Api.Controllers;

[ApiController]
[Route("api/ai")]
[EnableRateLimiting("ai-per-user")]
public sealed class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IValidator<AiChatRequest> _validator;

    public AiController(IAiService aiService, IValidator<AiChatRequest> validator)
    {
        _aiService = aiService;
        _validator = validator;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<AiChatResponse>> Ask(AiChatRequest request, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        // R4.3: the caller's role decides what the answer may contain. Cost
        // figures are dropped for a User before the response is composed.
        var response = await _aiService.AskAsync(request, includeCost: User.IsInRole("Admin"), ct);
        return Ok(response);
    }
}
