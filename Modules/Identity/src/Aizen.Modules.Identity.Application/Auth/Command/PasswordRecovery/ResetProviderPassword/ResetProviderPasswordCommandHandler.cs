using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResetProviderPassword;

public sealed class ResetProviderPasswordCommandHandler
    : AizenCommandHandler<ResetProviderPasswordCommand, ResetProviderPasswordResponse>
{
    private readonly IProviderPasswordRecoveryDomainService _service;

    public ResetProviderPasswordCommandHandler(IProviderPasswordRecoveryDomainService service)
    {
        _service = service;
    }

    public override async Task<ResetProviderPasswordResponse?> Handle(
        ResetProviderPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ResetAsync(request.ResetToken, request.NewPassword, cancellationToken);

        return new ResetProviderPasswordResponse
        {
            Success = result.Success,
            Message = result.Message,
        };
    }
}
