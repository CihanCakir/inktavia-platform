using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.CommissionBenefit;

/// <summary>
/// One hold against a <see cref="ProviderCommissionBenefitEntitlementEntity"/> (BE-P7): Reserved before checkout →
/// Consumed on success or Released on failure/cancel. <c>ContextRef</c> ties it to the offer/transaction for idempotency.
/// </summary>
[DocumentationInfo("Provider commission benefit usage entity",
    "Reserve → Consume/Release ledger row for a commission-benefit entitlement (GMV + benefit amount tracked).")]
public sealed class ProviderCommissionBenefitUsageEntity : AizenEntityWithAudit
{
    public long                             EntitlementId { get; private set; }
    public string                           ContextRef    { get; private set; } = default!;
    public decimal                          GmvAmount     { get; private set; }
    public decimal                          BenefitAmount { get; private set; }
    public CustomerBenefitReservationStatus Status        { get; private set; }
    public DateTime                         ReservedAtUtc { get; private set; }
    public DateTime?                        ConsumedAtUtc { get; private set; }
    public DateTime?                        ReleasedAtUtc { get; private set; }

    private ProviderCommissionBenefitUsageEntity() { }

    internal static ProviderCommissionBenefitUsageEntity CreateReserved(
        long entitlementId, string contextRef, decimal gmvAmount, decimal benefitAmount, DateTime atUtc)
        => new()
        {
            EntitlementId = entitlementId,
            ContextRef    = contextRef,
            GmvAmount     = gmvAmount,
            BenefitAmount = benefitAmount,
            Status        = CustomerBenefitReservationStatus.Reserved,
            ReservedAtUtc = atUtc,
            IsActive      = true,
        };

    internal void MarkConsumed(DateTime atUtc)
    {
        if (Status != CustomerBenefitReservationStatus.Reserved)
            throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitUsageInvalidState);
        Status        = CustomerBenefitReservationStatus.Consumed;
        ConsumedAtUtc = atUtc;
    }

    internal void MarkReleased(DateTime atUtc)
    {
        if (Status != CustomerBenefitReservationStatus.Reserved)
            throw new AizenBusinessException((int)PaymentErrorCode.ProviderCommissionBenefitUsageInvalidState);
        Status        = CustomerBenefitReservationStatus.Released;
        ReleasedAtUtc = atUtc;
    }
}
