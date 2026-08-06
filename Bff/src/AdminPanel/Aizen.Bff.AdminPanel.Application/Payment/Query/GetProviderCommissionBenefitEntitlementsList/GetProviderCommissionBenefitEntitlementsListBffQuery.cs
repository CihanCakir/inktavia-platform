using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderCommissionBenefitEntitlementsList;


// ─── Entitlement: List ───────────────────────────────────────────────────────
public sealed class GetProviderCommissionBenefitEntitlementsListBffQuery : AizenQuery<ProviderCommissionBenefitEntitlementListBffResult>
{
    public long? ProviderProfileId { get; init; }
    public long? BenefitRuleId     { get; init; }
    public bool? IsActive          { get; init; }
}
