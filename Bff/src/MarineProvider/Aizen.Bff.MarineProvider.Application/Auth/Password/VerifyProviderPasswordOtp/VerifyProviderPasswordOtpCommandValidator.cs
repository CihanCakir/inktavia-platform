using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Auth.Password.VerifyProviderPasswordOtp;

public sealed class VerifyProviderPasswordOtpCommandValidator : AizenValidator<VerifyProviderPasswordOtpCommand>
{
    public VerifyProviderPasswordOtpCommandValidator()
    {
        RuleFor(x => x.ResetRequestId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.OtpCode).NotEmpty().Length(6).Matches(@"^\d{6}$").WithMessage("Code must be 6 digits.");
    }
}
