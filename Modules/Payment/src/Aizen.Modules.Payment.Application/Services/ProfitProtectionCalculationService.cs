using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Wires the pure <see cref="ProfitProtectionEngine"/> to persistence (BE-P5): resolves the active policy, runs the
/// engine, and writes a <see cref="ProfitProtectionEvaluationLogEntity"/> for non-Approved outcomes
/// (ApprovedWithAdjustment / Rejected / ConfigurationError). <b>No payment economics snapshot is created here</b>
/// (snapshot = P8); on failure this log is the only record. The engine itself stays pure — all I/O lives here.
///
/// A policy CONFLICT (overlapping active policies) is caught and surfaced as a ConfigurationError decision (+ log),
/// per §19.11, rather than throwing to the caller.
/// </summary>
public sealed class ProfitProtectionCalculationService
{
    private readonly IProfitProtectionPolicyRepository        _policies;
    private readonly IProfitProtectionEvaluationLogRepository _logs;
    private readonly ILogger<ProfitProtectionCalculationService> _logger;

    public ProfitProtectionCalculationService(
        IProfitProtectionPolicyRepository policies,
        IProfitProtectionEvaluationLogRepository logs,
        ILogger<ProfitProtectionCalculationService> logger)
    {
        _policies = policies;
        _logs     = logs;
        _logger   = logger;
    }

    public async Task<ProfitProtectionEvaluation> EvaluateAsync(
        ProfitProtectionContext ctx, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        ProfitProtectionPolicyEntity? policy = null;
        try
        {
            policy = await _policies.ResolveAsync(ctx.CurrencyCode, now, ct);
        }
        catch (AizenBusinessException ex) when (ex.ErrorCode == (int)PaymentErrorCode.ProfitProtectionPolicyConflict)
        {
            _logger.LogError(ex, "Profit-protection policy conflict for {Currency} — returning ConfigurationError.", ctx.CurrencyCode);
            policy = null;   // engine → ConfigurationError
        }

        // Pure decision.
        var evaluation = ProfitProtectionEngine.Evaluate(ctx, policy);

        // Audit non-Approved outcomes; NO snapshot is created on failure (§7).
        if (evaluation.State != ProfitProtectionDecisionState.Approved)
        {
            await _logs.AddAsync(ProfitProtectionEvaluationLogEntity.Create(ctx, evaluation, now), ct);
            await _logs.SaveChangesAsync(ct);
        }

        return evaluation;
    }
}
