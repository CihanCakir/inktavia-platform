using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.OtpLogin.VerifyParticipantOtpLogin;

public sealed class VerifyParticipantOtpLoginCommandValidator : AizenValidator<VerifyParticipantOtpLoginCommand>
{
    public VerifyParticipantOtpLoginCommandValidator()
    {
        RuleFor(x => x.LoginRequestId).NotEmpty();
        RuleFor(x => x.OtpCode).NotEmpty();
    }
}
