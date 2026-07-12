using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.RequestProviderOtpLogin;

public sealed class RequestProviderOtpLoginCommandValidator : AizenValidator<RequestProviderOtpLoginCommand>
{
    public RequestProviderOtpLoginCommandValidator()
    {
        RuleFor(x => x.Channel).NotEmpty();
        RuleFor(x => x.Identifier).NotEmpty();
    }
}
