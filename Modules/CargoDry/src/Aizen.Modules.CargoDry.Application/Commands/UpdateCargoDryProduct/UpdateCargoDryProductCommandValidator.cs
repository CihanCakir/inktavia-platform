using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.UpdateCargoDryProduct;

public sealed class UpdateCargoDryProductCommandValidator
    : AbstractValidator<UpdateCargoDryProductCommand>
{
    public UpdateCargoDryProductCommandValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ValidityDays).InclusiveBetween(1, 3650);
        RuleFor(x => x.RetailPrice).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);

        RuleFor(x => x.DeviceType)
            .NotEmpty()
            .When(x => x.HasSmartDevice)
            .WithMessage("DeviceType is required when HasSmartDevice is true.");

        RuleFor(x => x.DeviceType)
            .MaximumLength(100)
            .When(x => x.DeviceType is not null);
    }
}
