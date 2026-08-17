using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendAdminOtpLogin;

public sealed class ResendAdminOtpLoginCommandValidator : AizenValidator<ResendAdminOtpLoginCommand>
{
    public ResendAdminOtpLoginCommandValidator()
    {
        RuleFor(x => x.LoginRequestId).NotEmpty();
    }
}
