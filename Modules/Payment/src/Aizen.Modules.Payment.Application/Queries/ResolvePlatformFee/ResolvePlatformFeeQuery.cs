using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Queries.ResolvePlatformFee;

/// <summary>
/// Admin/dev query: resolves the platform fee rule for a context and, when a base amount is supplied, computes the
/// net/vat/gross breakdown. Base defaults to 0 (rule-only preview).
/// </summary>
public sealed class ResolvePlatformFeeQuery : AizenQuery<PlatformFeeResolveResult>
{
    public string  CurrencyCode                 { get; init; } = "TRY";
    public string? CategoryCode                 { get; init; }
    public string? CustomerType                 { get; init; }
    public decimal CustomerPayableServiceAmount { get; init; } = 0m;
}

/// <summary>Full resolution + computed breakdown for admin/dev preview.</summary>
public sealed record PlatformFeeResolveResult(
    long             RuleId,
    string?          RuleCode,
    PlatformFeeModel Model,
    decimal?         Rate,
    decimal?         MinAmount,
    decimal?         MaxAmount,
    decimal?         FixedAmount,
    int              SpecificityRank,
    string           Source,
    decimal          FeeNet,
    decimal          FeeVat,
    decimal          FeeGross,
    decimal          VatRate,
    string           VatSource);
