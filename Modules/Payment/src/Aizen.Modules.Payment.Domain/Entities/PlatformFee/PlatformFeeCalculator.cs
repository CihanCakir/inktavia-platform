using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.PlatformFee;

/// <summary>
/// Net / VAT / gross platform fee breakdown (§13.3). All amounts pass through <see cref="MoneyMath"/> (2-dp
/// AwayFromZero). <see cref="VatSource"/> records where the VAT rate came from (Rule / ReferenceData / Default)
/// for audit — the definitive VAT treatment awaits YMM (§2.5).
/// </summary>
public sealed record PlatformFeeBreakdown(
    decimal Net,
    decimal Vat,
    decimal Gross,
    decimal VatRate,
    string  VatSource);

/// <summary>
/// Pure platform fee math (BE-P3, §4/§5). Computes the fee net for each model on
/// <c>base = CustomerPayableServiceAmount</c> (§19.13), and the net/vat/gross breakdown for a given VAT rate.
/// No DB, no config — the caller supplies the (already-resolved) rule and VAT rate. Fully unit-testable.
/// </summary>
public static class PlatformFeeCalculator
{
    /// <summary>
    /// Computes the fee net for the resolved rule on <paramref name="customerPayableServiceAmount"/>:
    /// Percentage → Round(base×Rate); Fixed → Round(FixedAmount); PercentageWithBounds → Clamp(Round(base×Rate),
    /// Min, Max); Waived → 0. Throws <see cref="PaymentErrorCode.PlatformFeeRuleInvalid"/> if a required parameter
    /// is missing (defensive — the entity factory already guarantees coherence).
    /// </summary>
    public static decimal ApplyNet(decimal customerPayableServiceAmount, PlatformFeeResolution rule)
    {
        switch (rule.Model)
        {
            case PlatformFeeModel.Percentage:
                Require(rule.Rate, "Rate");
                return MoneyMath.Round(customerPayableServiceAmount * rule.Rate!.Value);

            case PlatformFeeModel.Fixed:
                Require(rule.FixedAmount, "FixedAmount");
                return MoneyMath.Round(rule.FixedAmount!.Value);

            case PlatformFeeModel.PercentageWithBounds:
                Require(rule.Rate, "Rate");
                Require(rule.MinAmount, "MinAmount");
                Require(rule.MaxAmount, "MaxAmount");
                var raw = MoneyMath.Round(customerPayableServiceAmount * rule.Rate!.Value);
                return Clamp(raw, rule.MinAmount!.Value, rule.MaxAmount!.Value);

            case PlatformFeeModel.Waived:
                return 0m;

            default:
                throw new AizenBusinessException(
                    (int)PaymentErrorCode.PlatformFeeRuleInvalid, $"Unknown platform fee model '{rule.Model}'.");
        }
    }

    /// <summary>
    /// Net / VAT / gross for a resolved rule and VAT rate (§13.3): Net = ApplyNet; Vat = Round(Net × vatRate);
    /// Gross = Net + Vat. <paramref name="vatSource"/> is recorded for audit.
    /// </summary>
    public static PlatformFeeBreakdown ComputeBreakdown(
        decimal customerPayableServiceAmount,
        PlatformFeeResolution rule,
        decimal vatRate,
        string vatSource)
    {
        var net   = ApplyNet(customerPayableServiceAmount, rule);
        var vat   = MoneyMath.Round(net * vatRate);
        var gross = net + vat;   // both operands already 2-dp rounded → no drift
        return new PlatformFeeBreakdown(net, vat, gross, MoneyMath.RoundRate(vatRate), vatSource);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
        => value < min ? min : value > max ? max : value;

    private static void Require(decimal? value, string name)
    {
        if (value is null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.PlatformFeeRuleInvalid, $"Resolved platform fee rule is missing {name}.");
    }
}
