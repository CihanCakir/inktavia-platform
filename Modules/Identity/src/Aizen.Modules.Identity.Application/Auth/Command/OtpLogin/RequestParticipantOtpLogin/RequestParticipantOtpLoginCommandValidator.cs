using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.RequestParticipantOtpLogin;

public sealed class RequestParticipantOtpLoginCommandValidator : AizenValidator<RequestParticipantOtpLoginCommand>
{
    public RequestParticipantOtpLoginCommandValidator()
    {
        RuleFor(x => x.Channel).NotEmpty();
        RuleFor(x => x.Identifier).NotEmpty();
    }
}
