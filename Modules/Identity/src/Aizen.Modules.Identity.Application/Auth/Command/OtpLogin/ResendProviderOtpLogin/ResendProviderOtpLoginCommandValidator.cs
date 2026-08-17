using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendProviderOtpLogin;

public sealed class ResendProviderOtpLoginCommandValidator : AizenValidator<ResendProviderOtpLoginCommand>
{
    public ResendProviderOtpLoginCommandValidator()
    {
        RuleFor(x => x.LoginRequestId).NotEmpty();
    }
}
