using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;

/// <summary>
/// A customer's commercial-advantage control budget for one subscription period (§19.7). <b>NOT a wallet</b> — it only
/// caps how much benefit (platform-funded discount) may be applied. ELITE/top tiers are NOT unlimited: <see cref="FundedAmount"/>
/// is a finite <c>planPeriodRevenue × BenefitBudgetRate</c>. Reserve → Consume/Release adjusts Reserved/Consumed; the
/// discount can never exceed <see cref="RemainingAmount"/>. <see cref="Version"/> is an optimistic-concurrency token so two
/// concurrent checkouts cannot double-spend the same budget (§8/§19.15).
/// </summary>
[DocumentationInfo("Customer benefit budget entity",
    "Per-period commercial-advantage control budget (Funded/Reserved/Consumed/Remaining). Optimistic concurrency on " +
    "Version prevents double-spend. Not a wallet.")]
public sealed class CustomerBenefitBudgetEntity : AizenEntityWithAudit
{
    public long                       ParticipantPlanSubscriptionId { get; private set; }
    public long                       CustomerPlanId { get; private set; }
    public DateTime                   PeriodStart    { get; private set; }
    public DateTime                   PeriodEnd      { get; private set; }
    public decimal                    FundedAmount   { get; private set; }
    public decimal                    ReservedAmount { get; private set; }
    public decimal                    ConsumedAmount { get; private set; }
    public string                     CurrencyCode   { get; private set; } = "TRY";
    public CustomerBenefitBudgetStatus Status        { get; private set; }

    /// <summary>Optimistic-concurrency token (EF <c>IsConcurrencyToken</c>); incremented on every mutation.</summary>
    public long                       Version        { get; private set; }

    /// <summary>Computed: Funded − Reserved − Consumed. How much benefit can still be reserved.</summary>
    public decimal RemainingAmount => FundedAmount - ReservedAmount - ConsumedAmount;

    private CustomerBenefitBudgetEntity() { }

    public static CustomerBenefitBudgetEntity Create(
        long participantPlanSubscriptionId, long customerPlanId,
        DateTime periodStart, DateTime periodEnd, decimal fundedAmount, string currencyCode)
    {
        if (fundedAmount < 0m)
            throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitInsufficientRemaining,
                "FundedAmount must be ≥ 0.");

        return new CustomerBenefitBudgetEntity
        {
            ParticipantPlanSubscriptionId = participantPlanSubscriptionId,
            CustomerPlanId = customerPlanId,
            PeriodStart    = periodStart,
            PeriodEnd      = periodEnd,
            FundedAmount   = fundedAmount,
            ReservedAmount = 0m,
            ConsumedAmount = 0m,
            CurrencyCode   = currencyCode.ToUpperInvariant(),
            Status         = CustomerBenefitBudgetStatus.Active,
            Version        = 0,
            IsActive       = true,
        };
    }

    // ── Reserve / Consume / Release (§19.7) ─────────────────────────────────────

    /// <summary>Holds <paramref name="amount"/> against Remaining. Throws if it exceeds Remaining (ELITE ≠ unlimited).</summary>
    public CustomerBenefitReservationEntity Reserve(decimal amount, string contextRef, DateTime atUtc)
    {
        if (amount <= 0m)
            throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitInsufficientRemaining,
                "Reservation amount must be > 0.");
        if (amount > RemainingAmount)
            throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitInsufficientRemaining,
                $"Reservation {amount} exceeds RemainingAmount {RemainingAmount}.");

        ReservedAmount += amount;
        Version++;
        return CustomerBenefitReservationEntity.CreateReserved(Id, amount, contextRef, atUtc);
    }

    /// <summary>Converts a held reservation to consumed on successful payment.</summary>
    public void Consume(CustomerBenefitReservationEntity reservation, DateTime atUtc)
    {
        GuardOwned(reservation);
        reservation.MarkConsumed(atUtc);   // throws if not Reserved
        ReservedAmount -= reservation.Amount;
        ConsumedAmount += reservation.Amount;
        Version++;
    }

    /// <summary>Releases a held reservation back to Remaining on failure/timeout/cancel.</summary>
    public void Release(CustomerBenefitReservationEntity reservation, DateTime atUtc)
    {
        GuardOwned(reservation);
        reservation.MarkReleased(atUtc);   // throws if not Reserved
        ReservedAmount -= reservation.Amount;
        Version++;
    }

    private void GuardOwned(CustomerBenefitReservationEntity reservation)
    {
        if (reservation.BudgetId != Id)
            throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitReservationNotFound,
                "Reservation does not belong to this budget.");
    }
}
