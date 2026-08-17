using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.VerifyParticipantPasswordRecoveryOtp;

public sealed class VerifyParticipantPasswordRecoveryOtpCommandHandler
    : AizenCommandHandler<VerifyParticipantPasswordRecoveryOtpCommand, VerifyProviderPasswordRecoveryOtpResponse>
{
    private readonly IParticipantPasswordRecoveryDomainService _service;

    public VerifyParticipantPasswordRecoveryOtpCommandHandler(IParticipantPasswordRecoveryDomainService service)
    {
        _service = service;
    }

    public override async Task<VerifyProviderPasswordRecoveryOtpResponse?> Handle(
        VerifyParticipantPasswordRecoveryOtpCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.VerifyOtpAsync(request.ResetRequestId, request.OtpCode, cancellationToken);

        return new VerifyProviderPasswordRecoveryOtpResponse
        {
            Verified = result.Verified,
            ResetToken = result.ResetToken,
            ExpiresInSeconds = result.ExpiresInSeconds,
            Message = result.Verified
                ? "Code verified. You may now set a new password."
                : "The code is invalid or has expired. Please request a new code.",
        };
    }
}
