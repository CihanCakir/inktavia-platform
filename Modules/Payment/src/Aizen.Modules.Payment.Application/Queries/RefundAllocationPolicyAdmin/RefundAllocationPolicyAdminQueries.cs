using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.RefundAllocationPolicyAdmin;

internal static class RefundAllocationPolicyMapper
{
    public static RefundAllocationPolicyAdminDto Map(RefundAllocationPolicyEntity p) => new()
    {
        Id = p.Id, CurrencyCode = p.CurrencyCode, NegativeBalanceLimit = p.NegativeBalanceLimit,
        EffectiveFrom = p.EffectiveFrom, EffectiveTo = p.EffectiveTo, Status = (int)p.Status,
        PolicyCode = p.PolicyCode, PolicyName = p.PolicyName, Notes = p.Notes, IsActive = p.IsActive,
        Rules = p.Rules.Select(r => new RefundAllocationPolicyRuleDto
        {
            Cause = (int)r.Cause, PlatformFeeRefundMode = (int)r.PlatformFeeRefundMode, FixedPlatformFeeAmount = r.FixedPlatformFeeAmount,
        }).ToList(),
    };
}

// ─── List ────────────────────────────────────────────────────────────────────
public sealed class GetRefundAllocationPoliciesQuery : AizenQuery<List<RefundAllocationPolicyAdminDto>> { }

public sealed class GetRefundAllocationPoliciesQueryHandler
    : AizenQueryHandler<GetRefundAllocationPoliciesQuery, List<RefundAllocationPolicyAdminDto>>
{
    private readonly IRefundAllocationPolicyRepository _policies;
    public GetRefundAllocationPoliciesQueryHandler(IRefundAllocationPolicyRepository policies) => _policies = policies;

    public override async Task<List<RefundAllocationPolicyAdminDto>?> Handle(GetRefundAllocationPoliciesQuery request, CancellationToken ct)
        => (await _policies.GetAllAsync(ct)).Select(RefundAllocationPolicyMapper.Map).ToList();
}

// ─── By id ───────────────────────────────────────────────────────────────────
public sealed class GetRefundAllocationPolicyByIdQuery : AizenQuery<RefundAllocationPolicyAdminDto> { public long Id { get; init; } }

public sealed class GetRefundAllocationPolicyByIdQueryHandler
    : AizenQueryHandler<GetRefundAllocationPolicyByIdQuery, RefundAllocationPolicyAdminDto>
{
    private readonly IRefundAllocationPolicyRepository _policies;
    public GetRefundAllocationPolicyByIdQueryHandler(IRefundAllocationPolicyRepository policies) => _policies = policies;

    public override async Task<RefundAllocationPolicyAdminDto?> Handle(GetRefundAllocationPolicyByIdQuery request, CancellationToken ct)
    {
        var p = await _policies.GetByIdAsync(request.Id, ct);
        return p is null ? null : RefundAllocationPolicyMapper.Map(p);
    }
}

// ─── Resolve (active policy for a currency at an instant) ────────────────────
public sealed class ResolveRefundAllocationPolicyQuery : AizenQuery<RefundAllocationPolicyAdminDto>
{
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}

public sealed class ResolveRefundAllocationPolicyQueryHandler
    : AizenQueryHandler<ResolveRefundAllocationPolicyQuery, RefundAllocationPolicyAdminDto>
{
    private readonly IRefundAllocationPolicyRepository _policies;
    public ResolveRefundAllocationPolicyQueryHandler(IRefundAllocationPolicyRepository policies) => _policies = policies;

    public override async Task<RefundAllocationPolicyAdminDto?> Handle(ResolveRefundAllocationPolicyQuery request, CancellationToken ct)
    {
        var p = await _policies.ResolveAsync(request.CurrencyCode, request.AtUtc ?? DateTime.UtcNow, ct);
        return p is null ? null : RefundAllocationPolicyMapper.Map(p);
    }
}
