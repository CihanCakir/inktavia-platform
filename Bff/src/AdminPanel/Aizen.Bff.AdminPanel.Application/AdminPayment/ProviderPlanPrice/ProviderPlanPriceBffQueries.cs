using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProviderPlanPrice;

// ─── List prices for a plan ──────────────────────────────────────────────────
public sealed class GetProviderPlanPricesBffQuery : AizenQuery<GetProviderPlanPricesBffResponse>
{
    public long ProviderPlanId { get; init; }
}
public sealed class GetProviderPlanPricesBffResponse { public List<ProviderPlanPriceBffDto> Items { get; init; } = new(); }

[DocumentationInfo("Get provider-plan prices BFF query handler (BE-P4)",
    "Lists all versioned prices for a plan (GET /plan-prices/plan/{planId}). Read-only.")]
public sealed class GetProviderPlanPricesBffQueryHandler
    : AizenQueryHandler<GetProviderPlanPricesBffQuery, GetProviderPlanPricesBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public GetProviderPlanPricesBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<GetProviderPlanPricesBffResponse?> Handle(GetProviderPlanPricesBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetProviderPlanPricesForPlanAsync(request.ProviderPlanId, ct) };
}

// ─── Resolve (point-in-time price) ───────────────────────────────────────────
public sealed class ResolveProviderPlanPriceBffQuery : AizenQuery<ResolveProviderPlanPriceBffResponse>
{
    public long      ProviderPlanId { get; init; }
    public string    CurrencyCode   { get; init; } = "TRY";
    public string    BillingPeriod  { get; init; } = "Monthly";
    public DateTime? AtUtc          { get; init; }
}
public sealed class ResolveProviderPlanPriceBffResponse { public ProviderPlanPriceBffDto? Result { get; init; } }

[DocumentationInfo("Resolve provider-plan-price BFF query handler (BE-P4)",
    "Point-in-time single-active price resolution (GET /plan-prices/resolve). The resolver dev query.")]
public sealed class ResolveProviderPlanPriceBffQueryHandler
    : AizenQueryHandler<ResolveProviderPlanPriceBffQuery, ResolveProviderPlanPriceBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ResolveProviderPlanPriceBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ResolveProviderPlanPriceBffResponse?> Handle(ResolveProviderPlanPriceBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveProviderPlanPriceAsync(
            request.ProviderPlanId, request.CurrencyCode, request.BillingPeriod, request.AtUtc, ct) };
}

// ─── Upcoming price changes ──────────────────────────────────────────────────
public sealed class GetUpcomingPlanPriceChangesBffQuery : AizenQuery<GetUpcomingPlanPriceChangesBffResponse>
{
    public int WithinDays { get; init; } = 14;
}
public sealed class GetUpcomingPlanPriceChangesBffResponse { public List<UpcomingPriceChangeBffItem> Items { get; init; } = new(); }

[DocumentationInfo("Get upcoming plan-price changes BFF query handler (BE-P4)",
    "Lists subscriptions whose renewal price changes within N days (GET /plan-prices/upcoming-changes). Read-only.")]
public sealed class GetUpcomingPlanPriceChangesBffQueryHandler
    : AizenQueryHandler<GetUpcomingPlanPriceChangesBffQuery, GetUpcomingPlanPriceChangesBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public GetUpcomingPlanPriceChangesBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<GetUpcomingPlanPriceChangesBffResponse?> Handle(GetUpcomingPlanPriceChangesBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetUpcomingPlanPriceChangesAsync(request.WithinDays, ct) };
}
