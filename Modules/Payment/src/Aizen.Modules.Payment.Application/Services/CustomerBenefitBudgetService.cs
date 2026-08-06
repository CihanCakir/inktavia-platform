using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Reserve → Consume/Release operations on a <see cref="CustomerBenefitBudgetEntity"/> (§19.7). Concurrency-safe
/// (optimistic <c>Version</c> token → <see cref="PaymentErrorCode.CustomerBenefitConcurrencyConflict"/> on a clash) and
/// idempotent (a repeat with the same context-ref returns the existing reservation; a duplicate consume/release is a
/// no-op) so a duplicate webhook cannot double-consume. Reserve/consume/release ORDERING at acceptance is wired in P8.
///
/// <para>N4 (§19.7): when a reserve drops <c>RemainingAmount</c> to/below the configurable low threshold (or 0) it
/// publishes <see cref="CustomerBenefitBudgetLowMessage"/> ONCE per crossing (a <c>LowBudgetNotified</c> marker, re-armed
/// on release/recovery) so admins can top up. Additive, fire-and-forget — the alert never blocks reserve/consume.</para>
/// </summary>
public sealed class CustomerBenefitBudgetService
{
    private const string LowThresholdPercentKey = "Payment:BenefitBudgetLowThresholdPercent";
    private const decimal DefaultLowThresholdPercent = 10m;

    private readonly ICustomerBenefitBudgetRepository _repo;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IConfiguration _config;
    private readonly ILogger<CustomerBenefitBudgetService> _logger;

    public CustomerBenefitBudgetService(
        ICustomerBenefitBudgetRepository repo,
        IAizenMessagePublisher publisher,
        IConfiguration config,
        ILogger<CustomerBenefitBudgetService> logger)
    {
        _repo      = repo;
        _publisher = publisher;
        _config    = config;
        _logger    = logger;
    }

    private decimal ThresholdPercent =>
        _config.GetValue(LowThresholdPercentKey, DefaultLowThresholdPercent);

    private static decimal ThresholdAmountFor(CustomerBenefitBudgetEntity b, decimal percent) =>
        b.FundedAmount * (percent / 100m);

    /// <summary>Holds <paramref name="amount"/> against the budget before checkout. Idempotent per (budget, contextRef).</summary>
    public async Task<CustomerBenefitReservationEntity> ReserveAsync(
        long budgetId, decimal amount, string contextRef, CancellationToken ct = default)
    {
        var budget = await _repo.GetByIdAsync(budgetId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitBudgetNotFound);

        var existing = await _repo.GetReservationByContextRefAsync(budgetId, contextRef, ct);
        if (existing is not null)
            return existing;   // idempotent — same offer/txn already reserved

        var reservation = budget.Reserve(amount, contextRef, DateTime.UtcNow);   // throws if > Remaining
        await _repo.AddReservationAsync(reservation, ct);

        // N4 — the reserve just lowered Remaining: detect a low/exhausted crossing (once) and stamp the marker in the
        // same transaction; publish the admin alert AFTER the save, fire-and-forget.
        var percent    = ThresholdPercent;
        var threshold  = ThresholdAmountFor(budget, percent);
        var crossedLow = budget.ShouldNotifyLow(threshold);
        if (crossedLow) budget.MarkLowBudgetNotified();

        await _repo.SaveChangesConcurrencySafeAsync(ct);

        if (crossedLow)
            PublishBudgetLow(budget, threshold, percent, ct);

        return reservation;
    }

    /// <summary>Converts a reservation to consumed on successful payment. Duplicate consume is a no-op.</summary>
    public async Task<CustomerBenefitReservationEntity> ConsumeAsync(long reservationId, CancellationToken ct = default)
    {
        var reservation = await _repo.GetReservationByIdAsync(reservationId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitReservationNotFound);

        if (reservation.Status == CustomerBenefitReservationStatus.Consumed)
            return reservation;   // idempotent duplicate consume (e.g. duplicate webhook)

        var budget = await _repo.GetByIdAsync(reservation.BudgetId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitBudgetNotFound);

        budget.Consume(reservation, DateTime.UtcNow);   // moves reserved→consumed; Remaining unchanged
        await _repo.SaveChangesConcurrencySafeAsync(ct);
        return reservation;
    }

    /// <summary>Releases a reservation back to Remaining on failure/timeout/cancel. Duplicate release is a no-op.</summary>
    public async Task<CustomerBenefitReservationEntity> ReleaseAsync(long reservationId, CancellationToken ct = default)
    {
        var reservation = await _repo.GetReservationByIdAsync(reservationId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitReservationNotFound);

        if (reservation.Status == CustomerBenefitReservationStatus.Released)
            return reservation;   // idempotent

        var budget = await _repo.GetByIdAsync(reservation.BudgetId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.CustomerBenefitBudgetNotFound);

        budget.Release(reservation, DateTime.UtcNow);   // Remaining recovers

        // N4 — re-arm the low marker once remaining recovers above the threshold, so a later re-crossing alerts again.
        budget.ClearLowBudgetMarkerIfRecovered(ThresholdAmountFor(budget, ThresholdPercent));

        await _repo.SaveChangesConcurrencySafeAsync(ct);
        return reservation;
    }

    private void PublishBudgetLow(CustomerBenefitBudgetEntity budget, decimal threshold, decimal percent, CancellationToken ct)
    {
        _ = _publisher.PublishAsync(new CustomerBenefitBudgetLowMessage
        {
            BudgetId         = budget.Id,
            CustomerPlanId   = budget.CustomerPlanId,
            RemainingAmount  = budget.RemainingAmount,
            FundedAmount     = budget.FundedAmount,
            ThresholdAmount  = threshold,
            ThresholdPercent = percent,
            CurrencyCode     = budget.CurrencyCode,
            IsExhausted      = budget.IsExhausted,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception, "Failed to publish CustomerBenefitBudgetLowMessage for budget {BudgetId}.", budget.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "Benefit budget {BudgetId} low: remaining {Remaining} ≤ threshold {Threshold} ({Percent}%). Admin alert published.",
            budget.Id, budget.RemainingAmount, threshold, percent);
    }
}
