using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyAdminOtpLogin;

public sealed class VerifyAdminOtpLoginCommandValidator : AizenValidator<VerifyAdminOtpLoginCommand>
{
    public VerifyAdminOtpLoginCommandValidator()
    {
        RuleFor(x => x.LoginRequestId).NotEmpty();
        RuleFor(x => x.OtpCode).NotEmpty();
    }
}
