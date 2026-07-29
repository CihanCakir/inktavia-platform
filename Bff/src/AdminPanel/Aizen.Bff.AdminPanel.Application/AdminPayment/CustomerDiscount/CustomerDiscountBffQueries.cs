using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.CustomerDiscount;

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

[DocumentationInfo("Resolve customer-discount BFF query handler (BE-P6)",
    "Point-in-time customer-discount + funding-split preview (GET /customer-discounts/resolve). Read-only.")]
public sealed class ResolveCustomerDiscountBffQueryHandler
    : AizenQueryHandler<ResolveCustomerDiscountBffQuery, ResolveCustomerDiscountBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ResolveCustomerDiscountBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ResolveCustomerDiscountBffResponse?> Handle(ResolveCustomerDiscountBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveCustomerDiscountAsync(
            request.CustomerPlanId, request.CategoryCode, request.CurrencyCode,
            request.ServiceBaseAmount, request.ProviderConsent, request.ParticipantPlanSubscriptionId, ct) };
}
