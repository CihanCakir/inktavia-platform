using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.RefundAllocationPolicy;

// ─── List ────────────────────────────────────────────────────────────────────
public sealed class GetRefundAllocationPoliciesBffQuery : AizenQuery<GetRefundAllocationPoliciesBffResponse> { }
public sealed class GetRefundAllocationPoliciesBffResponse { public List<RefundAllocationPolicyAdminDto> Items { get; init; } = new(); }

[DocumentationInfo("Get refund-allocation policies BFF query handler (P10)",
    "Lists all refund-allocation policies (GET /admin/refund-allocation-policies). Read-only.")]
public sealed class GetRefundAllocationPoliciesBffQueryHandler
    : AizenQueryHandler<GetRefundAllocationPoliciesBffQuery, GetRefundAllocationPoliciesBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public GetRefundAllocationPoliciesBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<GetRefundAllocationPoliciesBffResponse?> Handle(GetRefundAllocationPoliciesBffQuery request, CancellationToken ct)
        => new() { Items = await _payment.GetRefundAllocationPoliciesAsync(ct) };
}

// ─── Resolve (point-in-time) ─────────────────────────────────────────────────
public sealed class ResolveRefundAllocationPolicyBffQuery : AizenQuery<ResolveRefundAllocationPolicyBffResponse>
{
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}
public sealed class ResolveRefundAllocationPolicyBffResponse { public RefundAllocationPolicyAdminDto? Result { get; init; } }

[DocumentationInfo("Resolve refund-allocation policy BFF query handler (P10)",
    "Point-in-time active refund-allocation policy resolution (GET /admin/refund-allocation-policies/resolve). Read-only.")]
public sealed class ResolveRefundAllocationPolicyBffQueryHandler
    : AizenQueryHandler<ResolveRefundAllocationPolicyBffQuery, ResolveRefundAllocationPolicyBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ResolveRefundAllocationPolicyBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ResolveRefundAllocationPolicyBffResponse?> Handle(ResolveRefundAllocationPolicyBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ResolveRefundAllocationPolicyAsync(request.CurrencyCode, request.AtUtc, ct) };
}

// ─── Get by id ───────────────────────────────────────────────────────────────
public sealed class GetRefundAllocationPolicyByIdBffQuery : AizenQuery<GetRefundAllocationPolicyByIdBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetRefundAllocationPolicyByIdBffResponse { public RefundAllocationPolicyAdminDto? Result { get; init; } }

[DocumentationInfo("Get refund-allocation policy by id BFF query handler (P10)",
    "Reads a single refund-allocation policy (GET /admin/refund-allocation-policies/{id}). Read-only.")]
public sealed class GetRefundAllocationPolicyByIdBffQueryHandler
    : AizenQueryHandler<GetRefundAllocationPolicyByIdBffQuery, GetRefundAllocationPolicyByIdBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public GetRefundAllocationPolicyByIdBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<GetRefundAllocationPolicyByIdBffResponse?> Handle(GetRefundAllocationPolicyByIdBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetRefundAllocationPolicyByIdAsync(request.Id, ct) };
}
