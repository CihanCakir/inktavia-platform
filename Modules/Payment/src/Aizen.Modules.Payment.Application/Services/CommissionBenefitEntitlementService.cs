using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Reserve → Consume/Release on a <see cref="ProviderCommissionBenefitEntitlementEntity"/> (BE-P7, §5). Concurrency-safe
/// (optimistic <c>Version</c> → <see cref="PaymentErrorCode.ProviderCommissionBenefitConcurrencyConflict"/>) and idempotent
/// (repeat context-ref returns the existing usage; duplicate consume/release is a no-op) so concurrent offers can't
/// double-reserve and a duplicate webhook can't double-consume. The resolver stays pure; this is its side-effecting
/// counterpart. Ordering vs the customer-benefit-budget reserve is P8.
/// </summary>
public sealed class CommissionBenefitEntitlementService
{
    private readonly IProviderCommissionBenefitEntitlementRepository _repo;
    public CommissionBenefitEntitlementService(IProviderCommissionBenefitEntitlementRepository repo) => _repo = repo;

    public async Task<ProviderCommissionBenefitUsageEntity> ReserveAsync(
        long entitlementId, decimal gmvAmount, decimal benefitAmount, string contextRef, CancellationToken ct = default)
    {
        var entitlement = await _repo.GetByIdAsync(entitlementId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitEntitlementNotFound);

        var existing = await _repo.GetUsageByContextRefAsync(entitlementId, contextRef, ct);
        if (existing is not null)
            return existing;   // idempotent — same offer already reserved

        var usage = entitlement.Reserve(gmvAmount, benefitAmount, contextRef, DateTime.UtcNow);   // throws if exhausted
        await _repo.AddUsageAsync(usage, ct);
        await _repo.SaveChangesConcurrencySafeAsync(ct);
        return usage;
    }

    public async Task<ProviderCommissionBenefitUsageEntity> ConsumeAsync(long usageId, CancellationToken ct = default)
    {
        var usage = await _repo.GetUsageByIdAsync(usageId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitUsageNotFound);

        if (usage.Status == CustomerBenefitReservationStatus.Consumed)
            return usage;   // idempotent duplicate consume

        var entitlement = await _repo.GetByIdAsync(usage.EntitlementId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitEntitlementNotFound);

        entitlement.Consume(usage, DateTime.UtcNow);
        await _repo.SaveChangesConcurrencySafeAsync(ct);
        return usage;
    }

    public async Task<ProviderCommissionBenefitUsageEntity> ReleaseAsync(long usageId, CancellationToken ct = default)
    {
        var usage = await _repo.GetUsageByIdAsync(usageId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitUsageNotFound);

        if (usage.Status == CustomerBenefitReservationStatus.Released)
            return usage;   // idempotent

        var entitlement = await _repo.GetByIdAsync(usage.EntitlementId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitEntitlementNotFound);

        entitlement.Release(usage, DateTime.UtcNow);
        await _repo.SaveChangesConcurrencySafeAsync(ct);
        return usage;
    }
}
