using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.CreateCargoDryProduct;

public sealed class CreateCargoDryProductCommandValidator
    : AbstractValidator<CreateCargoDryProductCommand>
{
    public CreateCargoDryProductCommandValidator()
    {
        RuleFor(x => x.ProductCode)
            .NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_-]+$")
            .WithMessage("ProductCode must be uppercase alphanumeric (A-Z, 0-9, _, -).");

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().MaximumLength(1000);

        RuleFor(x => x.ValidityDays)
            .InclusiveBetween(1, 3650)
            .WithMessage("ValidityDays must be between 1 and 3650.");

        RuleFor(x => x.RetailPrice)
            .GreaterThan(0);

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().Length(3)
            .WithMessage("CurrencyCode must be a 3-letter ISO code (e.g. USD).");

        RuleFor(x => x.DeviceType)
            .NotEmpty()
            .When(x => x.HasSmartDevice)
            .WithMessage("DeviceType is required when HasSmartDevice is true.");

        RuleFor(x => x.DeviceType)
            .MaximumLength(100)
            .When(x => x.DeviceType is not null);
    }
}
