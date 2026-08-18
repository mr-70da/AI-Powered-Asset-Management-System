using FluentValidation;

namespace Kinana.AssetManagement.Application.Ai;

public sealed class AiChatRequestValidator : AbstractValidator<AiChatRequest>
{
    public AiChatRequestValidator()
    {
        RuleFor(r => r.Question)
            .NotEmpty()
            .WithMessage("Please enter a question.");

        RuleFor(r => r.Question)
            .MaximumLength(500)
            .WithMessage("The question must be 500 characters or fewer.");
    }
}
