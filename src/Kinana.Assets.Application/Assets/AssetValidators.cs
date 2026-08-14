using FluentValidation;

namespace Kinana.AssetManagement.Application.Assets;

public sealed class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.AssetCode)
            .NotEmpty()
            .WithMessage("Asset code is required.")
            .MaximumLength(50);

        RuleFor(x => x.AssetName)
            .NotEmpty()
            .WithMessage("Asset name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.AssetTypeId).GreaterThan(0);

        RuleFor(x => x.Manufacturer).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);

        RuleFor(x => x.PurchaseCost).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .MaximumLength(30);
    }
}

public sealed class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.AssetCode)
            .NotEmpty()
            .WithMessage("Asset code is required.")
            .MaximumLength(50);

        RuleFor(x => x.AssetName)
            .NotEmpty()
            .WithMessage("Asset name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.AssetTypeId).GreaterThan(0);

        RuleFor(x => x.Manufacturer).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);

        RuleFor(x => x.PurchaseCost).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Status)
            .NotEmpty()
            .MaximumLength(30);
    }
}

public sealed class TransferAssetRequestValidator : AbstractValidator<TransferAssetRequest>
{
    public TransferAssetRequestValidator()
    {
        RuleFor(x => x.TransferDate)
            .NotEmpty()
            .WithMessage("Transfer date is required.")
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Transfer date cannot be in the future.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required.")
            .MaximumLength(500);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .WithMessage("RowVersion is required. Fetch the asset first to get its current version.");
    }
}
