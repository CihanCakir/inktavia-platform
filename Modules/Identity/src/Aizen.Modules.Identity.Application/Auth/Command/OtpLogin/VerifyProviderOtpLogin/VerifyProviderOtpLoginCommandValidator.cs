using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyProviderOtpLogin;

public sealed class VerifyProviderOtpLoginCommandValidator : AizenValidator<VerifyProviderOtpLoginCommand>
{
    public VerifyProviderOtpLoginCommandValidator()
    {
        RuleFor(x => x.LoginRequestId).NotEmpty();
        RuleFor(x => x.OtpCode).NotEmpty();
    }
}
