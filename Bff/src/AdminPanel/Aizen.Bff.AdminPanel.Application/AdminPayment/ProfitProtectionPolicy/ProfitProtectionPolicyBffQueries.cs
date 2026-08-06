using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProfitProtectionPolicy;

// ─── Resolve (active policy preview) ─────────────────────────────────────────
public sealed class ResolveProfitProtectionPolicyBffQuery : AizenQuery<ResolveProfitProtectionPolicyBffResponse>
{
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}
public sealed class ResolveProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyResolveBffResult? Result { get; init; } }

[DocumentationInfo("Resolve profit-protection policy BFF query handler (BE-P5)",
    "Resolves the single active policy for a currency at an instant (GET /profit-protection/resolve). Read-only.")]
public sealed class ResolveProfitProtectionPolicyBffQueryHandler
    : AizenQueryHandler<ResolveProfitProtectionPolicyBffQuery, ResolveProfitProtectionPolicyBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ResolveProfitProtectionPolicyBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ResolveProfitProtectionPolicyBffResponse?> Handle(ResolveProfitProtectionPolicyBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveProfitProtectionPolicyAsync(request.CurrencyCode, request.AtUtc, ct) };
}

// ─── List (version history — no paging) ──────────────────────────────────────
public sealed class GetProfitProtectionPoliciesListBffQuery : AizenQuery<GetProfitProtectionPoliciesListBffResponse>
{
    public string? CurrencyCode { get; init; }
    public string? Status       { get; init; }
    public bool?   IsActive     { get; init; }
}
public sealed class GetProfitProtectionPoliciesListBffResponse { public ProfitProtectionPolicyListBffResult Result { get; init; } = default!; }

[DocumentationInfo("Get profit-protection policies list BFF query handler (BE-P5)",
    "Returns the profit-protection policy version history (no paging — one active per currency plus historical " +
    "versions) with optional currency / status / active filters, forwarded as plain strings to the Payment module. " +
    "Sorted by currency then EffectiveFrom desc. Read-only.")]
public sealed class GetProfitProtectionPoliciesListBffQueryHandler
    : AizenQueryHandler<GetProfitProtectionPoliciesListBffQuery, GetProfitProtectionPoliciesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProfitProtectionPoliciesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetProfitProtectionPoliciesListBffResponse?> Handle(GetProfitProtectionPoliciesListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListProfitProtectionPoliciesAsync(
            request.CurrencyCode, request.Status, request.IsActive, ct) };
}

// ─── Detail (by id) ──────────────────────────────────────────────────────────
public sealed class GetProfitProtectionPolicyDetailBffQuery : AizenQuery<GetProfitProtectionPolicyDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetProfitProtectionPolicyDetailBffResponse { public ProfitProtectionPolicyDetailBffDto? Policy { get; init; } }

[DocumentationInfo("Get profit-protection policy detail BFF query handler (BE-P5)",
    "Fetches a single profit-protection policy by ID from the Payment module (GET /profit-protection/policies/{id}). " +
    "A ProfitProtectionPolicyNotFound surfaces through the envelope. Read-only.")]
public sealed class GetProfitProtectionPolicyDetailBffQueryHandler
    : AizenQueryHandler<GetProfitProtectionPolicyDetailBffQuery, GetProfitProtectionPolicyDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProfitProtectionPolicyDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetProfitProtectionPolicyDetailBffResponse?> Handle(GetProfitProtectionPolicyDetailBffQuery request, CancellationToken ct)
        => new() { Policy = await _payment.GetProfitProtectionPolicyDetailAsync(request.Id, ct) };
}
