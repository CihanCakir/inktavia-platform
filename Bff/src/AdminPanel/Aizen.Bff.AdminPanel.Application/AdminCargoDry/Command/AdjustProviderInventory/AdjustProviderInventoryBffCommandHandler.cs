using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.AdjustProviderInventory;

[DocumentationInfo("Adjust provider inventory BFF command handler",
    "Forwards inventory adjustment request to the CargoDry admin inventory adjust endpoint. Phase 2.")]
public sealed class AdjustProviderInventoryBffCommandHandler
    : AizenCommandHandler<AdjustProviderInventoryBffCommand, AdjustProviderInventoryBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public AdjustProviderInventoryBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<AdjustProviderInventoryBffCommandResponse> Handle(
        AdjustProviderInventoryBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new AdjustProviderInventoryBffRequest
        {
            ProviderProfileId  = request.ProviderProfileId,
            ProductCode        = request.ProductCode,
            BatchCode          = request.BatchCode,
            AdjustmentQuantity = request.AdjustmentQuantity,
            Reason             = request.Reason,
        };

        var result = await _remote.AdjustProviderInventoryAsync(remoteRequest, ct);

        return new AdjustProviderInventoryBffCommandResponse { UpdatedInventory = result };
    }
}
