using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.RequestAdminOtpLogin;

public sealed class RequestAdminOtpLoginCommandValidator : AizenValidator<RequestAdminOtpLoginCommand>
{
    public RequestAdminOtpLoginCommandValidator()
    {
        RuleFor(x => x.Channel).NotEmpty();
        RuleFor(x => x.Identifier).NotEmpty();
    }
}
