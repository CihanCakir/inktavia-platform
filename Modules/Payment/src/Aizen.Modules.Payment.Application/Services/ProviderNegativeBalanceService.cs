using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// BE-P10 §7.3/§7.4 — the provider negative-balance recovery seam shared by the payout handlers (offset-before-payout +
/// limit block) and the BE-P8 acceptance gate (limit block). A provider whose negative balance exceeds the policy limit
/// is blocked from receiving payouts and from being accepted onto new work until it is brought back within the limit
/// (auto-offset from future payouts, or an admin manual adjustment).
/// </summary>
public sealed class ProviderNegativeBalanceService
{
    private readonly IProviderBalanceRepository            _balances;
    private readonly ILogger<ProviderNegativeBalanceService> _logger;

    public ProviderNegativeBalanceService(
        IProviderBalanceRepository balances, ILogger<ProviderNegativeBalanceService> logger)
    {
        _balances = balances;
        _logger   = logger;
    }

    /// <summary>
    /// §7.3 step 3 — offsets the provider's negative balance from an available payout BEFORE it is disbursed. Throws
    /// <see cref="PaymentErrorCode.ProviderNegativeBalanceLimitExceeded"/> when the balance is over the policy limit and
    /// <paramref name="allowOverLimit"/> is false (admin override is audited by the caller). Returns the offset applied.
    /// </summary>
    public async Task<decimal> OffsetBeforePayoutAsync(
        long providerProfileId, string currency, decimal payoutAmount, bool allowOverLimit, CancellationToken ct)
    {
        var balance = await _balances.GetByProviderAsync(providerProfileId, currency, ct);
        if (balance is null || balance.NegativeAmount <= 0m) return 0m;   // nothing owed → normal payout

        if (balance.IsOverLimit() && !allowOverLimit)
            throw new AizenBusinessException((int)PaymentErrorCode.ProviderNegativeBalanceLimitExceeded,
                $"Provider {providerProfileId} negative balance {balance.NegativeAmount} exceeds limit {balance.NegativeBalanceLimit} — payout blocked.");

        var offset = balance.OffsetFromPayout(payoutAmount, DateTime.UtcNow);
        if (offset > 0m)
        {
            _balances.Update(balance);
            _logger.LogInformation(
                "Payout offset {Offset} applied to provider {Provider} negative balance (remaining {Remaining}).",
                offset, providerProfileId, balance.NegativeAmount);
        }
        return offset;
    }

    /// <summary>§7.4 — true when the provider's negative balance is over the policy limit (blocks BE-P8 acceptance).</summary>
    public async Task<bool> IsOverLimitAsync(long providerProfileId, string currency, CancellationToken ct)
    {
        var balance = await _balances.GetByProviderAsync(providerProfileId, currency, ct);
        return balance is not null && balance.IsOverLimit();
    }
}
