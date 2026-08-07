using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// BE-S9 (§20.12) — insert-only audit of a LINE-level profit-protection evaluation, written for <b>non-Approved</b> cases
/// (Rejected / ConfigurationError) — mirrors the P5 <see cref="ProfitProtectionEvaluationLogEntity"/>, which logs the
/// non-Approved transaction decision without a snapshot. On a line-level failure NO payment economics snapshot / escrow is
/// created and SR acceptance rolls back; this log is the only record of which line breached and why. No mutators.
/// </summary>
[DocumentationInfo("Line profit protection evaluation log entity",
    "Insert-only audit of a line-level profit-protection decision (Rejected/ConfigurationError). Captures the failing " +
    "line count/refs + primary breach code + reason. Written when a line fails BEFORE the transaction gates; no snapshot (§20.12).")]
[NoMessagebusSync] // domain-authored immutable financial evaluation log — never generically writable
public sealed class LineProfitProtectionEvaluationLogEntity : AizenEntityWithAudit
{
    public long                          ServiceRequestId { get; private set; }
    public long                          OfferId          { get; private set; }
    public long?                         PolicyId         { get; private set; }
    public string                        CurrencyCode     { get; private set; } = "TRY";
    public ProfitProtectionDecisionState DecisionState    { get; private set; }
    public DateTime                      EvaluatedAtUtc   { get; private set; }

    public int      LineCount           { get; private set; }
    public int      FailedLineCount     { get; private set; }
    public int?     PrimaryBreachCode   { get; private set; }
    /// <summary>Comma-joined refs of the lines that breached (quick audit; the full per-line reasons are in <see cref="Reason"/>).</summary>
    public string?  FailedLineRefs      { get; private set; }
    public string?  Reason              { get; private set; }

    private LineProfitProtectionEvaluationLogEntity() { }

    public static LineProfitProtectionEvaluationLogEntity Create(
        long serviceRequestId, long offerId, string currencyCode, long? policyId,
        LineProfitProtectionEvaluation eval, DateTime evaluatedAtUtc)
    {
        var failed = eval.Lines.Where(l => !l.Passed).ToList();
        return new LineProfitProtectionEvaluationLogEntity
        {
            ServiceRequestId  = serviceRequestId,
            OfferId           = offerId,
            PolicyId          = policyId,
            CurrencyCode      = currencyCode.ToUpperInvariant(),
            DecisionState     = eval.State,
            EvaluatedAtUtc    = evaluatedAtUtc.Kind == DateTimeKind.Utc ? evaluatedAtUtc : DateTime.SpecifyKind(evaluatedAtUtc, DateTimeKind.Utc),
            LineCount         = eval.Lines.Count,
            FailedLineCount   = failed.Count,
            PrimaryBreachCode = eval.PrimaryErrorCode,
            FailedLineRefs    = failed.Count > 0 ? string.Join(",", failed.Select(l => l.LineRef)) : null,
            Reason            = eval.Reason,
            IsActive          = true,
        };
    }
}
