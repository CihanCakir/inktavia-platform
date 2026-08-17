using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PlatformFee;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Resolves a platform fee rule for a context and computes its net / VAT / gross breakdown (BE-P3, §4/§5).
///
/// VAT rate precedence (source recorded on the result):
///   1. rule.VatRate            → source "Rule"
///   2. ReferenceData param     → source "ReferenceData"   (PLATFORM_FEE_VAT_RATE)
///   3. configurable fallback   → source "Default"          (NOT a hardcoded business constant — a safety default;
///                                the definitive VAT treatment awaits YMM, §2.5)
///
/// Pure math lives in <see cref="PlatformFeeCalculator"/>; this service only wires resolution + VAT sourcing.
/// It performs NO writes (resolution is side-effect-free); snapshot persistence happens at acceptance (P8).
/// </summary>
public sealed class PlatformFeeCalculationService
{
    private readonly IPlatformFeeRuleRepository _rules;
    private readonly ISystemParameterReferenceService _systemParams;
    private readonly ILogger<PlatformFeeCalculationService> _logger;

    private const string  VatRateParamKey = "PLATFORM_FEE_VAT_RATE";
    private const decimal DefaultVatRate  = 0.20m; // safety fallback only (Turkish KDV 20%); tune via ReferenceData / YMM

    public PlatformFeeCalculationService(
        IPlatformFeeRuleRepository rules,
        ISystemParameterReferenceService systemParams,
        ILogger<PlatformFeeCalculationService> logger)
    {
        _rules        = rules;
        _systemParams = systemParams;
        _logger       = logger;
    }

    /// <summary>
    /// Resolves the fee rule for <paramref name="ctx"/> and computes the breakdown on
    /// <paramref name="customerPayableServiceAmount"/> (§19.13 base). Throws PlatformFeeRuleNotFound when nothing
    /// (incl. Global) matches. Resolution may throw PlatformFeeRuleConflict on a fail-loud tie.
    /// </summary>
    public async Task<PlatformFeeCalculationResult> CalculateAsync(
        decimal customerPayableServiceAmount,
        PlatformFeeResolveContext ctx,
        CancellationToken ct = default)
    {
        var resolution = await _rules.ResolveAsync(ctx, DateTime.UtcNow, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PlatformFeeRuleNotFound);

        var (vatRate, vatSource) = await ResolveVatRateAsync(resolution, ct);
        var breakdown = PlatformFeeCalculator.ComputeBreakdown(
            customerPayableServiceAmount, resolution, vatRate, vatSource);

        return new PlatformFeeCalculationResult(resolution, breakdown);
    }

    private async Task<(decimal Rate, string Source)> ResolveVatRateAsync(
        PlatformFeeResolution resolution, CancellationToken ct)
    {
        if (resolution.VatRate is { } ruleRate && ruleRate >= 0m)
            return (ruleRate, "Rule");

        var param = await _systemParams.GetDecimalAsync(VatRateParamKey, ct);
        if (param is { } p && p is >= 0m and <= 1m)
            return (p, "ReferenceData");

        _logger.LogWarning(
            "Platform fee VAT rate not set on rule or ReferenceData ({Key}); falling back to default {Default} (YMM-pending).",
            VatRateParamKey, DefaultVatRate);
        return (DefaultVatRate, "Default");
    }
}

/// <summary>Resolved rule + its net/vat/gross breakdown.</summary>
public sealed record PlatformFeeCalculationResult(
    PlatformFeeResolution Resolution,
    PlatformFeeBreakdown  Breakdown);
