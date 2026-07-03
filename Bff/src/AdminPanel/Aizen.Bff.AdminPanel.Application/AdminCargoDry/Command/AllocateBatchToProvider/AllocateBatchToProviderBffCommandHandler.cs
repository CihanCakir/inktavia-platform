using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.AllocateBatchToProvider;

[DocumentationInfo("Allocate batch to provider BFF command handler",
    "Forwards batch allocation request to the CargoDry admin inventory allocate endpoint. Phase 2.")]
public sealed class AllocateBatchToProviderBffCommandHandler
    : AizenCommandHandler<AllocateBatchToProviderBffCommand, AllocateBatchToProviderBffCommandResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public AllocateBatchToProviderBffCommandHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<AllocateBatchToProviderBffCommandResponse> Handle(
        AllocateBatchToProviderBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new AllocateBatchToProviderBffRequest
        {
            BatchCode              = request.BatchCode,
            ProviderProfileId      = request.ProviderProfileId,
            CommercialModel        = request.CommercialModel,
            SalesChannel           = request.SalesChannel,
            ConsignmentAgreementId = request.ConsignmentAgreementId,
            WarehouseId            = request.WarehouseId,
            Note                   = request.Note,
        };

        var result = await _remote.AllocateBatchToProviderAsync(remoteRequest, ct);

        return new AllocateBatchToProviderBffCommandResponse { Result = result };
    }
}
