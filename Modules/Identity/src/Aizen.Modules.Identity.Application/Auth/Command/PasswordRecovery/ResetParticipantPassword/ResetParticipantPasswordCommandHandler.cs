using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResetParticipantPassword;

public sealed class ResetParticipantPasswordCommandHandler
    : AizenCommandHandler<ResetParticipantPasswordCommand, ResetProviderPasswordResponse>
{
    private readonly IParticipantPasswordRecoveryDomainService _service;

    public ResetParticipantPasswordCommandHandler(IParticipantPasswordRecoveryDomainService service)
    {
        _service = service;
    }

    public override async Task<ResetProviderPasswordResponse?> Handle(
        ResetParticipantPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ResetAsync(request.ResetToken, request.NewPassword, cancellationToken);

        return new ResetProviderPasswordResponse
        {
            Success = result.Success,
            Message = result.Message,
        };
    }
}
