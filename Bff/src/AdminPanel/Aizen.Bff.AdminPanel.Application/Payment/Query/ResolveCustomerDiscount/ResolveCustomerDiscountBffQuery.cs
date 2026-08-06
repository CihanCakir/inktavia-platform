using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveCustomerDiscount;


// ─── Resolve (discount + funding preview) ────────────────────────────────────
public sealed class ResolveCustomerDiscountBffQuery : AizenQuery<ResolveCustomerDiscountBffResponse>
{
    public long?    CustomerPlanId                { get; init; }
    public string?  CategoryCode                  { get; init; }
    public string   CurrencyCode                  { get; init; } = "TRY";
    public decimal  ServiceBaseAmount             { get; init; }
    public bool     ProviderConsent               { get; init; }
    public long?    ParticipantPlanSubscriptionId { get; init; }
}
public sealed class ResolveCustomerDiscountBffResponse { public CustomerDiscountResolveBffResult? Result { get; init; } }
