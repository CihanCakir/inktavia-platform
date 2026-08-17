namespace Aizen.Modules.Payment.Domain.Money;

/// <summary>
/// Central rounding helper for all Payment economics (§13.6).
///
/// TRY money amounts are rounded to 2 decimals, away-from-zero. Storage stays numeric(18,4).
/// Commission and platform fee are each rounded <b>separately</b> via <see cref="Round"/>;
/// derived amounts (e.g. ProviderNet = ServiceAmount − CommissionAmount) are the difference of
/// two already-rounded values and are therefore not re-rounded — no component may drift by a kuruş.
///
/// This is a pure static helper — no DI. Callers must route every monetary computation through it
/// so the zero-tolerance invariants in <c>PaymentEconomicsSnapshotEntity</c> hold exactly.
/// </summary>
public static class MoneyMath
{
    /// <summary>TRY money amounts: 2 decimals, away-from-zero.</summary>
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    /// <summary>Rates keep 4 decimals, away-from-zero.</summary>
    public static decimal RoundRate(decimal rate) => Math.Round(rate, 4, MidpointRounding.AwayFromZero);
}
