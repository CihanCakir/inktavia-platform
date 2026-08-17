using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;

/// <summary>
/// A single hold against a <see cref="CustomerBenefitBudgetEntity"/> (§19.7): Reserved before checkout → Consumed on
/// success or Released on failure/cancel. <c>ContextRef</c> ties it to the offer/transaction for idempotency.
/// </summary>
[DocumentationInfo("Customer benefit reservation entity",
    "Reserve → Consume/Release lifecycle hold against a benefit budget. ContextRef links to the offer/transaction.")]
public sealed class CustomerBenefitReservationEntity : AizenEntityWithAudit
{
    public long                             BudgetId       { get; private set; }
    public decimal                          Amount         { get; private set; }
    public CustomerBenefitReservationStatus Status         { get; private set; }
    public string                           ContextRef     { get; private set; } = default!;
    public DateTime                         ReservedAtUtc  { get; private set; }
    public DateTime?                        ConsumedAtUtc  { get; private set; }
    public DateTime?                        ReleasedAtUtc  { get; private set; }

    private CustomerBenefitReservationEntity() { }

    internal static CustomerBenefitReservationEntity CreateReserved(
        long budgetId, decimal amount, string contextRef, DateTime atUtc)
        => new()
        {
            BudgetId      = budgetId,
            Amount        = amount,
            Status        = CustomerBenefitReservationStatus.Reserved,
            ContextRef    = contextRef,
            ReservedAtUtc = atUtc,
            IsActive      = true,
        };

    internal void MarkConsumed(DateTime atUtc)
    {
        if (Status != CustomerBenefitReservationStatus.Reserved)
            throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitReservationInvalidState);
        Status        = CustomerBenefitReservationStatus.Consumed;
        ConsumedAtUtc = atUtc;
    }

    internal void MarkReleased(DateTime atUtc)
    {
        if (Status != CustomerBenefitReservationStatus.Reserved)
            throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitReservationInvalidState);
        Status        = CustomerBenefitReservationStatus.Released;
        ReleasedAtUtc = atUtc;
    }
}
