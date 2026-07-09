using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Phone.VerifyProviderPhoneOtp;

public sealed class VerifyProviderPhoneOtpCommandValidator : AizenValidator<VerifyProviderPhoneOtpCommand>
{
    public VerifyProviderPhoneOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Otp).GreaterThan(0);
        RuleFor(x => x.ValidationGuid).NotEmpty();
    }
}
