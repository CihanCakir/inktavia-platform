using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.PlatformFeeRule;

// ─── Resolve (point-in-time preview) ─────────────────────────────────────────
public sealed class ResolvePlatformFeeBffQuery : AizenQuery<ResolvePlatformFeeBffResponse>
{
    public string   CurrencyCode                 { get; init; } = "TRY";
    public string?  CategoryCode                 { get; init; }
    public string?  CustomerType                 { get; init; }
    public decimal  CustomerPayableServiceAmount { get; init; }
}
public sealed class ResolvePlatformFeeBffResponse { public PlatformFeeResolveBffResult? Result { get; init; } }

[DocumentationInfo("Resolve platform-fee BFF query handler (BE-P3)",
    "Point-in-time platform-fee resolution preview (GET /platform-fee/resolve). Read-only.")]
public sealed class ResolvePlatformFeeBffQueryHandler
    : AizenQueryHandler<ResolvePlatformFeeBffQuery, ResolvePlatformFeeBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolvePlatformFeeBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolvePlatformFeeBffResponse?> Handle(ResolvePlatformFeeBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolvePlatformFeeAsync(
            request.CurrencyCode, request.CategoryCode, request.CustomerType, request.CustomerPayableServiceAmount, ct) };
}

// ─── List (paged + filters) ──────────────────────────────────────────────────
public sealed class GetPlatformFeeRulesListBffQuery : AizenQuery<GetPlatformFeeRulesListBffResponse>
{
    public string?  Model        { get; init; }
    public string?  Status       { get; init; }
    public string?  CurrencyCode { get; init; }
    public string?  CategoryCode { get; init; }
    public string?  CustomerType { get; init; }
    public int      Page         { get; init; } = 1;
    public int      PageSize     { get; init; } = 20;
}
public sealed class GetPlatformFeeRulesListBffResponse { public PlatformFeeRulesListBffResult Result { get; init; } = default!; }

[DocumentationInfo("Get platform-fee rules list BFF query handler (BE-P3)",
    "Returns a paged list of platform-fee rules with optional Model/Status/Currency/Category/CustomerType filters, " +
    "forwarded as plain strings to the Payment module (enum parsing handled by the module's own converter). Read-only.")]
public sealed class GetPlatformFeeRulesListBffQueryHandler
    : AizenQueryHandler<GetPlatformFeeRulesListBffQuery, GetPlatformFeeRulesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPlatformFeeRulesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPlatformFeeRulesListBffResponse?> Handle(GetPlatformFeeRulesListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListPlatformFeeRulesAsync(
            request.Model, request.Status, request.CurrencyCode, request.CategoryCode, request.CustomerType,
            request.Page, request.PageSize, ct) };
}

// ─── Detail (by id) ──────────────────────────────────────────────────────────
public sealed class GetPlatformFeeRuleDetailBffQuery : AizenQuery<GetPlatformFeeRuleDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetPlatformFeeRuleDetailBffResponse { public PlatformFeeRuleDetailBffDto? Rule { get; init; } }

[DocumentationInfo("Get platform-fee rule detail BFF query handler (BE-P3)",
    "Fetches a single platform-fee rule by ID from the Payment module (GET /platform-fee/rules/{id}). " +
    "A PlatformFeeRuleNotFound surfaces through the envelope. Read-only.")]
public sealed class GetPlatformFeeRuleDetailBffQueryHandler
    : AizenQueryHandler<GetPlatformFeeRuleDetailBffQuery, GetPlatformFeeRuleDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPlatformFeeRuleDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPlatformFeeRuleDetailBffResponse?> Handle(GetPlatformFeeRuleDetailBffQuery request, CancellationToken ct)
        => new() { Rule = await _payment.GetPlatformFeeRuleDetailAsync(request.Id, ct) };
}

// ─── Stats (KPI strip) ───────────────────────────────────────────────────────
public sealed class GetPlatformFeeRuleStatsBffQuery : AizenQuery<GetPlatformFeeRuleStatsBffResponse>;
public sealed class GetPlatformFeeRuleStatsBffResponse { public PlatformFeeRuleStatsBffDto? Stats { get; init; } }

[DocumentationInfo("Get platform-fee rule stats BFF query handler (BE-P3)",
    "Returns KPI counts (total / active / per-model) for the admin platform-fee rules dashboard strip. Read-only.")]
public sealed class GetPlatformFeeRuleStatsBffQueryHandler
    : AizenQueryHandler<GetPlatformFeeRuleStatsBffQuery, GetPlatformFeeRuleStatsBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPlatformFeeRuleStatsBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPlatformFeeRuleStatsBffResponse?> Handle(GetPlatformFeeRuleStatsBffQuery request, CancellationToken ct)
        => new() { Stats = await _payment.GetPlatformFeeRuleStatsAsync(ct) };
}
