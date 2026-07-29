using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Commands.CommissionBenefit;

// ── Grant ────────────────────────────────────────────────────────────────────

public sealed class GrantProviderCommissionBenefitEntitlementCommand : AizenCommand<GrantEntitlementResult>
{
    public required long     ProviderProfileId  { get; init; }
    public required long     BenefitRuleId      { get; init; }
    public required DateTime GrantedFrom        { get; init; }
    public DateTime?         GrantedTo          { get; init; }
    public long?             UsageLimit         { get; init; }
    public decimal?          MaximumEligibleGMV { get; init; }
}

public sealed record GrantEntitlementResult(long Id, string EntitlementCode);

[DocumentationInfo("GrantProviderCommissionBenefitEntitlementCommandHandler",
    "Admin grants a commission-benefit entitlement to a provider for a rule. Throws RuleNotFound if the rule is missing.")]
public sealed class GrantProviderCommissionBenefitEntitlementCommandHandler
    : AizenCommandHandler<GrantProviderCommissionBenefitEntitlementCommand, GrantEntitlementResult>
{
    private readonly IProviderCommissionBenefitEntitlementRepository _entitlements;
    private readonly IProviderCommissionBenefitRuleRepository _rules;

    public GrantProviderCommissionBenefitEntitlementCommandHandler(
        IProviderCommissionBenefitEntitlementRepository entitlements, IProviderCommissionBenefitRuleRepository rules)
    {
        _entitlements = entitlements;
        _rules = rules;
    }

    public override async Task<GrantEntitlementResult?> Handle(
        GrantProviderCommissionBenefitEntitlementCommand request, CancellationToken ct)
    {
        _ = await _rules.GetByIdAsync(request.BenefitRuleId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitRuleNotFound);

        var code = await _entitlements.GenerateEntitlementCodeAsync(ct);
        var entitlement = ProviderCommissionBenefitEntitlementEntity.Grant(
            code, request.ProviderProfileId, request.BenefitRuleId,
            request.GrantedFrom.ToUniversalTime(), request.GrantedTo?.ToUniversalTime(),
            request.UsageLimit, request.MaximumEligibleGMV);

        await _entitlements.AddEntitlementAsync(entitlement, ct);
        await _entitlements.SaveChangesAsync(ct);
        return new GrantEntitlementResult(entitlement.Id, code);
    }
}

// ── Revoke ───────────────────────────────────────────────────────────────────

public sealed class RevokeProviderCommissionBenefitEntitlementCommand : AizenCommand<RevokeEntitlementResult>
{
    public required long Id { get; init; }
}

public sealed record RevokeEntitlementResult(long Id, string? EntitlementCode);

[DocumentationInfo("RevokeProviderCommissionBenefitEntitlementCommandHandler",
    "Admin revokes an entitlement (Status=Revoked). Throws EntitlementNotFound if missing.")]
public sealed class RevokeProviderCommissionBenefitEntitlementCommandHandler
    : AizenCommandHandler<RevokeProviderCommissionBenefitEntitlementCommand, RevokeEntitlementResult>
{
    private readonly IProviderCommissionBenefitEntitlementRepository _entitlements;
    public RevokeProviderCommissionBenefitEntitlementCommandHandler(IProviderCommissionBenefitEntitlementRepository e) => _entitlements = e;

    public override async Task<RevokeEntitlementResult?> Handle(
        RevokeProviderCommissionBenefitEntitlementCommand request, CancellationToken ct)
    {
        var entitlement = await _entitlements.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitEntitlementNotFound);
        entitlement.Revoke();
        _entitlements.UpdateEntitlement(entitlement);
        await _entitlements.SaveChangesAsync(ct);
        return new RevokeEntitlementResult(entitlement.Id, entitlement.EntitlementCode);
    }
}

// ── Reserve / Consume / Release ──────────────────────────────────────────────

public sealed class ReserveCommissionBenefitCommand : AizenCommand<CommissionBenefitUsageResult>
{
    public required long    EntitlementId { get; init; }
    public required decimal GmvAmount     { get; init; }
    public required decimal BenefitAmount { get; init; }
    public required string  ContextRef    { get; init; }
}

public sealed class ConsumeCommissionBenefitCommand : AizenCommand<CommissionBenefitUsageResult>
{
    public required long UsageId { get; init; }
}

public sealed class ReleaseCommissionBenefitCommand : AizenCommand<CommissionBenefitUsageResult>
{
    public required long UsageId { get; init; }
}

public sealed record CommissionBenefitUsageResult(long UsageId, long EntitlementId, decimal GmvAmount, decimal BenefitAmount, string Status);

[DocumentationInfo("ReserveCommissionBenefitCommandHandler", "Holds one application + GMV against an entitlement (idempotent, concurrency-safe).")]
public sealed class ReserveCommissionBenefitCommandHandler : AizenCommandHandler<ReserveCommissionBenefitCommand, CommissionBenefitUsageResult>
{
    private readonly CommissionBenefitEntitlementService _service;
    public ReserveCommissionBenefitCommandHandler(CommissionBenefitEntitlementService service) => _service = service;

    public override async Task<CommissionBenefitUsageResult?> Handle(ReserveCommissionBenefitCommand request, CancellationToken ct)
    {
        var u = await _service.ReserveAsync(request.EntitlementId, request.GmvAmount, request.BenefitAmount, request.ContextRef, ct);
        return new CommissionBenefitUsageResult(u.Id, u.EntitlementId, u.GmvAmount, u.BenefitAmount, u.Status.ToString());
    }
}

[DocumentationInfo("ConsumeCommissionBenefitCommandHandler", "Consumes a usage on successful payment (idempotent).")]
public sealed class ConsumeCommissionBenefitCommandHandler : AizenCommandHandler<ConsumeCommissionBenefitCommand, CommissionBenefitUsageResult>
{
    private readonly CommissionBenefitEntitlementService _service;
    public ConsumeCommissionBenefitCommandHandler(CommissionBenefitEntitlementService service) => _service = service;

    public override async Task<CommissionBenefitUsageResult?> Handle(ConsumeCommissionBenefitCommand request, CancellationToken ct)
    {
        var u = await _service.ConsumeAsync(request.UsageId, ct);
        return new CommissionBenefitUsageResult(u.Id, u.EntitlementId, u.GmvAmount, u.BenefitAmount, u.Status.ToString());
    }
}

[DocumentationInfo("ReleaseCommissionBenefitCommandHandler", "Releases a usage on failure/cancel (idempotent).")]
public sealed class ReleaseCommissionBenefitCommandHandler : AizenCommandHandler<ReleaseCommissionBenefitCommand, CommissionBenefitUsageResult>
{
    private readonly CommissionBenefitEntitlementService _service;
    public ReleaseCommissionBenefitCommandHandler(CommissionBenefitEntitlementService service) => _service = service;

    public override async Task<CommissionBenefitUsageResult?> Handle(ReleaseCommissionBenefitCommand request, CancellationToken ct)
    {
        var u = await _service.ReleaseAsync(request.UsageId, ct);
        return new CommissionBenefitUsageResult(u.Id, u.EntitlementId, u.GmvAmount, u.BenefitAmount, u.Status.ToString());
    }
}
