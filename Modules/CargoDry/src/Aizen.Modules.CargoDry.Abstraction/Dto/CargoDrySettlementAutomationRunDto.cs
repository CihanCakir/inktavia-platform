using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// DTO for a CargoDry monthly settlement automation run record.
/// Phase 6 (July 2026).
/// </summary>
public sealed class CargoDrySettlementAutomationRunDto
{
    public long                                    Id                        { get; init; }
    public string                                  RunCode                   { get; init; } = default!;
    public int                                     TargetYearMonth           { get; init; }
    public CargoDrySettlementAutomationMode        Mode                      { get; init; }
    public CargoDrySettlementAutomationRunStatus   Status                    { get; init; }
    public bool                                    AutoCompletePayout        { get; init; }
    public bool                                    AutoPreparePayment        { get; init; }
    public bool                                    AutoPrepareInvoice        { get; init; }
    public long                                    TriggeredByUserId         { get; init; }
    public DateTime                                TriggeredAtUtc            { get; init; }
    public DateTime?                               CompletedAtUtc            { get; init; }
    public long?                                   DurationMs                { get; init; }
    public int                                     TotalSettlementsFound     { get; init; }
    public int                                     TotalSettlementsEligible  { get; init; }
    public int                                     TotalSettlementsProcessed { get; init; }
    public int                                     TotalSettlementsSkipped   { get; init; }
    public int                                     TotalSettlementsErrored   { get; init; }
    public string?                                 Note                      { get; init; }
    public string?                                 ErrorSummary              { get; init; }
    public DateTime                                CreatedAtUtc              { get; init; }
    public IReadOnlyList<CargoDrySettlementAutomationRunItemDto> RunItems    { get; init; } = [];
}

/// <summary>
/// DTO for a single per-settlement item within an automation run.
/// Phase 6 (July 2026).
/// </summary>
public sealed class CargoDrySettlementAutomationRunItemDto
{
    public long                                     Id                   { get; init; }
    public long                                     RunId                { get; init; }
    public long                                     SettlementId         { get; init; }
    public string                                   SettlementCode       { get; init; } = default!;
    public long                                     ProviderProfileId    { get; init; }
    public string                                   ProductCode          { get; init; } = default!;
    public string                                   CurrencyCode         { get; init; } = default!;
    public int                                      PeriodYearMonth      { get; init; }
    public CargoDrySellThroughSettlementStatus      StatusBefore         { get; init; }
    public string                                   Action               { get; init; } = default!;
    public bool                                     Success              { get; init; }
    public string?                                  ErrorMessage         { get; init; }
    public int                                      AttributionsResolved { get; init; }
    public int                                      AttributionsSkipped  { get; init; }
    public int                                      AttributionsErrored  { get; init; }
    public DateTime                                 ProcessedAtUtc       { get; init; }
}

/// <summary>
/// Dry-run preview result for a single settlement that would be processed by the automation.
/// Phase 6 (July 2026).
/// </summary>
public sealed class CargoDrySettlementAutomationPreviewItemDto
{
    public long                                     SettlementId                   { get; init; }
    public string                                   SettlementCode                 { get; init; } = default!;
    public long                                     ProviderProfileId              { get; init; }
    public string                                   ProductCode                    { get; init; } = default!;
    public string                                   CurrencyCode                   { get; init; } = default!;
    public int                                      PeriodYearMonth                { get; init; }
    public CargoDrySellThroughSettlementStatus      CurrentStatus                  { get; init; }

    /// <summary>Number of attributions currently unresolved (null financial amounts).</summary>
    public int                                      UnresolvedAttributionCount     { get; init; }

    /// <summary>Total attribution count for this settlement.</summary>
    public int                                      TotalAttributionCount          { get; init; }

    /// <summary>Predicted action the automation would take in Live mode.</summary>
    public string                                   PredictedAction                { get; init; } = default!;

    /// <summary>Whether this settlement is eligible for automated processing.</summary>
    public bool                                     IsEligible                     { get; init; }

    /// <summary>Reasons why this settlement is not eligible (if IsEligible = false).</summary>
    public IReadOnlyList<string>                    IneligibilityReasons           { get; init; } = [];
}

/// <summary>
/// Full dry-run preview response for a target year-month.
/// Phase 6 (July 2026).
/// </summary>
public sealed class CargoDrySettlementAutomationPreviewDto
{
    public int                                              TargetYearMonth           { get; init; }
    public int                                              TotalSettlementsFound     { get; init; }
    public int                                              TotalEligible             { get; init; }
    public int                                              TotalIneligible           { get; init; }
    public int                                              TotalWouldMarkReady       { get; init; }
    public int                                              TotalWouldPreparePayment  { get; init; }
    public int                                              TotalWouldPrepareInvoice  { get; init; }
    public bool                                             AutoPreparePayment        { get; init; }
    public bool                                             AutoPrepareInvoice        { get; init; }
    public IReadOnlyList<CargoDrySettlementAutomationPreviewItemDto> Items            { get; init; } = [];
}
