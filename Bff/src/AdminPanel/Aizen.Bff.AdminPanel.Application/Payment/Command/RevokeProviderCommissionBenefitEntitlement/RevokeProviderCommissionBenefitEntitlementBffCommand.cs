using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.RevokeProviderCommissionBenefitEntitlement;


// ─── Entitlement: Revoke ─────────────────────────────────────────────────────
public sealed class RevokeProviderCommissionBenefitEntitlementBffCommand : AizenCommand<RevokeProviderCommissionBenefitEntitlementBffResponse>
{
    public long Id { get; init; }
}
public sealed class RevokeProviderCommissionBenefitEntitlementBffResponse { public RevokeEntitlementBffResult Result { get; init; } = default!; }
