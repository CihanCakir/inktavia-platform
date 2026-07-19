using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Bff.MarineProvider.Application.Auth;

public sealed class ResendProviderPasswordOtpCommandValidator : AizenValidator<ResendProviderPasswordOtpCommand>
{
    public ResendProviderPasswordOtpCommandValidator()
    {
        RuleFor(x => x.ResetRequestId).NotEmpty().MaximumLength(128);
    }
}
