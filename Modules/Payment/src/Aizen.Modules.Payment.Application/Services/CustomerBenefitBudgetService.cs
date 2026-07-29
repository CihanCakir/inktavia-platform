using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Reserve → Consume/Release operations on a <see cref="CustomerBenefitBudgetEntity"/> (§19.7). Concurrency-safe
/// (optimistic <c>Version</c> token → <see cref="PaymentErrorCode.CustomerBenefitConcurrencyConflict"/> on a clash) and
/// idempotent (a repeat with the same context-ref returns the existing reservation; a duplicate consume/release is a
/// no-op) so a duplicate webhook cannot double-consume. Reserve/consume/release ORDERING at acceptance is wired in P8.
/// </summary>
public sealed class CustomerBenefitBudgetService
{
    private readonly ICustomerBenefitBudgetRepository _repo;
    public CustomerBenefitBudgetService(ICustomerBenefitBudgetRepository repo) => _repo = repo;

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
        await _repo.SaveChangesConcurrencySafeAsync(ct);
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

        budget.Consume(reservation, DateTime.UtcNow);
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

        budget.Release(reservation, DateTime.UtcNow);
        await _repo.SaveChangesConcurrencySafeAsync(ct);
        return reservation;
    }
}
