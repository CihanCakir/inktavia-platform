using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderCommissionBenefitEntitlementDetail;


// ─── Entitlement: Detail ─────────────────────────────────────────────────────
public sealed class GetProviderCommissionBenefitEntitlementDetailBffQuery : AizenQuery<ProviderCommissionBenefitEntitlementDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class ProviderCommissionBenefitEntitlementDetailBffResponse { public ProviderCommissionBenefitEntitlementDetailBffDto? Result { get; init; } }
