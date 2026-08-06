using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePartCommercialTerm;


// ─── PartCommercialTerm: Deactivate ──────────────────────────────────────────
public sealed class DeactivatePartCommercialTermBffCommand : AizenCommand<DeactivatePartCommercialTermBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivatePartCommercialTermBffResponse { public bool Result { get; init; } }
