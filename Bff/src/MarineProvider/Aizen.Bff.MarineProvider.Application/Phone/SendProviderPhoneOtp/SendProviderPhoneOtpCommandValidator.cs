using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Phone.SendProviderPhoneOtp;

public sealed class SendProviderPhoneOtpCommandValidator : AizenValidator<SendProviderPhoneOtpCommand>
{
    public SendProviderPhoneOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(32);
    }
}
