using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivatePartCommercialTerm;


// ─── PartCommercialTerm: Reactivate ──────────────────────────────────────────
public sealed class ReactivatePartCommercialTermBffCommand : AizenCommand<ReactivatePartCommercialTermBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivatePartCommercialTermBffResponse { public bool Result { get; init; } }
