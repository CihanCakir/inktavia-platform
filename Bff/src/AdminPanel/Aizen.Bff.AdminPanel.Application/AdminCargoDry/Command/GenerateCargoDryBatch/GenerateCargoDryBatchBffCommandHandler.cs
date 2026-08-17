using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.GenerateCargoDryBatch;

[DocumentationInfo("Generate CargoDry batch BFF command handler", "Calls the CargoDry admin batch generate endpoint and returns the new batch code and generated kit count.")]
public sealed class GenerateCargoDryBatchBffCommandHandler
    : AizenCommandHandler<GenerateCargoDryBatchBffCommand, GenerateCargoDryBatchBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public GenerateCargoDryBatchBffCommandHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GenerateCargoDryBatchBffResponse?> Handle(
        GenerateCargoDryBatchBffCommand request, CancellationToken ct)
    {
        var result = await _remote.GenerateBatchAsync(
            new GenerateBatchBffRequest
            {
                ProductCode = request.ProductCode,
                Count       = request.Count,
            }, ct);

        return new GenerateCargoDryBatchBffResponse { Result = result };
    }
}
