using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDryRenewalInvoice;

[DocumentationInfo("Prepare CargoDry renewal invoice BFF command handler",
    "Calls the CargoDry admin renewals/{id}/invoice POST endpoint. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryRenewalInvoiceBffCommandHandler
    : AizenCommandHandler<PrepareCargoDryRenewalInvoiceBffCommand, PrepareCargoDryRenewalInvoiceBffCommandResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public PrepareCargoDryRenewalInvoiceBffCommandHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<PrepareCargoDryRenewalInvoiceBffCommandResponse?> Handle(
        PrepareCargoDryRenewalInvoiceBffCommand request, CancellationToken ct)
    {
        var result = await _remote.PrepareRenewalInvoiceAsync(
            request.RenewalPreparationId,
            new PrepareRenewalInvoiceBffRequest { Note = request.Note },
            ct);

        return new PrepareCargoDryRenewalInvoiceBffCommandResponse { Preparation = result };
    }
}
