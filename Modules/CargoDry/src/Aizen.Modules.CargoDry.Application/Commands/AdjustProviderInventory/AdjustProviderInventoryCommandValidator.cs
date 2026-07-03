using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.AdjustProviderInventory;

public sealed class AdjustProviderInventoryCommandValidator
    : AbstractValidator<AdjustProviderInventoryCommand>
{
    public AdjustProviderInventoryCommandValidator()
    {
        RuleFor(x => x.ProviderProfileId)
            .GreaterThan(0).WithMessage("ProviderProfileId must be a positive value.");

        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("ProductCode is required.");

        RuleFor(x => x.AdjustmentQuantity)
            .NotEqual(0).WithMessage("AdjustmentQuantity must be non-zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required for a manual inventory adjustment.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
