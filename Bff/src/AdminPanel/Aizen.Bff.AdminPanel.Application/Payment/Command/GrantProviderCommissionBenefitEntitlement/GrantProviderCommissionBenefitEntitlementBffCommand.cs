using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.GrantProviderCommissionBenefitEntitlement;


// ─── Entitlement: Grant ──────────────────────────────────────────────────────
public sealed class GrantProviderCommissionBenefitEntitlementBffCommand : AizenCommand<GrantProviderCommissionBenefitEntitlementBffResponse>
{
    public GrantProviderCommissionBenefitEntitlementBffRequest Body { get; init; } = default!;
}
public sealed class GrantProviderCommissionBenefitEntitlementBffResponse { public GrantEntitlementBffResult Result { get; init; } = default!; }
