using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.CompleteCargoDryRenewal;

[DocumentationInfo("Complete CargoDry renewal BFF command handler",
    "Calls the CargoDry admin renewals/{id}/complete POST endpoint. " +
    "Phase 11 (July 2026).")]
public sealed class CompleteCargoDryRenewalBffCommandHandler
    : AizenCommandHandler<CompleteCargoDryRenewalBffCommand, CompleteCargoDryRenewalBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public CompleteCargoDryRenewalBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<CompleteCargoDryRenewalBffCommandResponse?> Handle(
        CompleteCargoDryRenewalBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CompleteRenewalAsync(
            request.RenewalPreparationId,
            new CompleteRenewalBffRequest
            {
                ManualPaymentReference = request.ManualPaymentReference,
                Note                   = request.Note,
            }, ct);

        return new CompleteCargoDryRenewalBffCommandResponse { Preparation = result };
    }
}
