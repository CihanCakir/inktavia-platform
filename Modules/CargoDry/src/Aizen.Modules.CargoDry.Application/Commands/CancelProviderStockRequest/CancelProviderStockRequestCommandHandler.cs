using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.CancelProviderStockRequest;

public sealed class CancelProviderStockRequestCommandHandler : AizenCommandHandler<CancelProviderStockRequestCommand, bool>
{
    private readonly ICargoDryStockRequestRepository _requests;

    public CancelProviderStockRequestCommandHandler(ICargoDryStockRequestRepository requests)
        => _requests = requests;

    public override async Task<bool> Handle(CancelProviderStockRequestCommand request, CancellationToken ct)
    {
        var entity = await _requests.GetByIdAsync(request.RequestId, ct)
            ?? throw new AizenBusinessException("Stock request not found.");

        if (entity.ProviderProfileId != request.ProviderProfileId)
            throw new AizenBusinessException("Stock request not found.");

        entity.Cancel(request.Reason);
        await _requests.SaveChangesAsync(ct);
        return true;
    }
}
