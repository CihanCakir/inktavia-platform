using FluentValidation;

namespace Aizen.Modules.Payment.Application.Commands.RegisterSubMerchant;

public sealed class RegisterSubMerchantCommandValidator : AbstractValidator<RegisterSubMerchantCommand>
{
    private static readonly string[] ValidSubMerchantTypes =
        ["PERSONAL", "PRIVATE_COMPANY", "LIMITED_OR_JOINT_STOCK_COMPANY"];

    public RegisterSubMerchantCommandValidator()
    {
        RuleFor(x => x.ProviderProfileId)
            .GreaterThan(0).WithMessage("ProviderProfileId must be greater than zero.");

        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("LegalName is required.")
            .MaximumLength(200).WithMessage("LegalName must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Iban)
            .NotEmpty().WithMessage("IBAN is required.");

        RuleFor(x => x.SubMerchantType)
            .NotEmpty().WithMessage("SubMerchantType is required.")
            .Must(t => ValidSubMerchantTypes.Contains(t))
            .WithMessage("SubMerchantType must be PERSONAL, PRIVATE_COMPANY, or LIMITED_OR_JOINT_STOCK_COMPANY.");
    }
}
