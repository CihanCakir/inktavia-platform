using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.ResendParticipantOtpLogin;

public sealed class ResendParticipantOtpLoginCommandValidator : AizenValidator<ResendParticipantOtpLoginCommand>
{
    public ResendParticipantOtpLoginCommandValidator()
    {
        RuleFor(x => x.LoginRequestId).NotEmpty();
    }
}
