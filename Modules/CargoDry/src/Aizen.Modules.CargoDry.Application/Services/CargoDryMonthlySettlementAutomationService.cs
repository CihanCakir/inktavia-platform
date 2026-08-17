using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDrySettlementInvoice;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDrySettlementPayment;
using Aizen.Modules.CargoDry.Application.Commands.ResolveMonthlySellThroughSettlement;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Implements the CargoDry monthly settlement automation.
///
/// Hard invariants preserved throughout this implementation:
///   - AutoCompletePayout is NEVER triggered. No payout lifecycle commands are called.
///   - DryRun mode never persists or mutates any data.
///   - Live mode dispatches existing CQRS command handlers via ISender.
///   - The service never marks any settlement as Settled.
///   - The service never calls Approve/Processing/Complete/Fail payout commands.
///   - Errors on individual settlements are caught, recorded, and processing continues.
///
/// Phase 6 (July 2026): CargoDry Monthly Settlement Automation Job.
/// </summary>
[DocumentationInfo("CargoDry Monthly Settlement Automation Service",
    "Implements dry-run preview and live execution for the monthly settlement automation. " +
    "Dispatches existing CQRS handlers — never bypasses them. " +
    "AutoCompletePayout is always false. Never marks settlements Settled. " +
    "Phase 6 (July 2026).")]
public sealed class CargoDryMonthlySettlementAutomationService : ICargoDryMonthlySettlementAutomationService
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDrySalesAttributionRepository      _attributions;
    private readonly ICargoDrySettlementAutomationRunRepository _runs;
    private readonly ISender                                  _sender;
    private readonly ILogger<CargoDryMonthlySettlementAutomationService> _logger;

    // Statuses eligible for automation processing
    private static readonly CargoDrySellThroughSettlementStatus[] EligibleStatuses =
    [
        CargoDrySellThroughSettlementStatus.Pending,
        CargoDrySellThroughSettlementStatus.ReadyForSettlement,
    ];

    // Action label constants — stored in run items for transparency
    private static class Actions
    {
        public const string WouldMarkReadyForSettlement   = "DryRun:WouldMarkReadyForSettlement";
        public const string WouldPreparePayment           = "DryRun:WouldPreparePayment";
        public const string WouldPrepareInvoice           = "DryRun:WouldPrepareInvoice";
        public const string WouldSkipUnresolved           = "DryRun:Skipped:UnresolvedAttributions";
        public const string WouldSkipAlreadyReady         = "DryRun:Skipped:AlreadyReadyForSettlement";
        public const string WouldSkipScheduled            = "DryRun:Skipped:AlreadyScheduled";
        public const string MarkedReadyForSettlement      = "MarkedReadyForSettlement";
        public const string PaymentPrepared               = "PaymentPrepared";
        public const string InvoicePrepared               = "InvoicePrepared";
        public const string SkippedUnresolved             = "Skipped:UnresolvedAttributions";
        public const string SkippedAlreadyReady           = "Skipped:AlreadyReadyForSettlement";
        public const string SkippedAlreadyScheduled       = "Skipped:AlreadyScheduled";
        public const string SkippedAutoPaymentDisabled    = "Skipped:AutoPreparePaymentDisabled";
        public const string ErrorPrefix                   = "Error:";
    }

    public CargoDryMonthlySettlementAutomationService(
        ICargoDrySellThroughSettlementRepository     settlements,
        ICargoDrySalesAttributionRepository          attributions,
        ICargoDrySettlementAutomationRunRepository   runs,
        ISender                                      sender,
        ILogger<CargoDryMonthlySettlementAutomationService> logger)
    {
        _settlements  = settlements;
        _attributions = attributions;
        _runs         = runs;
        _sender       = sender;
        _logger       = logger;
    }

    /// <inheritdoc/>
    public async Task<CargoDrySettlementAutomationPreviewDto> GetPreviewAsync(
        int               targetYearMonth,
        bool              autoPreparePayment,
        bool              autoPrepareInvoice,
        CancellationToken ct)
    {
        var settlementsInScope = await _settlements.GetForYearMonthAsync(
            targetYearMonth, EligibleStatuses, ct);

        var items        = new List<CargoDrySettlementAutomationPreviewItemDto>();
        var wouldReady   = 0;
        var wouldPayment = 0;
        var wouldInvoice = 0;

        foreach (var s in settlementsInScope)
        {
            var periodYearMonth = s.PeriodStartUtc.Year * 100 + s.PeriodStartUtc.Month;
            var allAttributions = await _attributions.GetBySettlementIdAsync(s.Id, ct);
            var unresolved      = allAttributions.Count(a => a.ResolvedRate is null);

            string  predictedAction;
            bool    isEligible = true;
            var     reasons    = new List<string>();

            if (s.Status == CargoDrySellThroughSettlementStatus.Pending)
            {
                if (unresolved > 0)
                {
                    predictedAction = Actions.WouldSkipUnresolved;
                    isEligible      = false;
                    reasons.Add($"{unresolved} of {allAttributions.Count} attributions have unresolved financials.");
                }
                else
                {
                    predictedAction = Actions.WouldMarkReadyForSettlement;
                    wouldReady++;

                    if (autoPreparePayment)
                    {
                        predictedAction = $"{predictedAction} → {Actions.WouldPreparePayment}";
                        wouldPayment++;

                        if (autoPrepareInvoice)
                        {
                            predictedAction = $"{predictedAction} → {Actions.WouldPrepareInvoice}";
                            wouldInvoice++;
                        }
                    }
                }
            }
            else if (s.Status == CargoDrySellThroughSettlementStatus.ReadyForSettlement)
            {
                if (!autoPreparePayment)
                {
                    predictedAction = Actions.WouldSkipAlreadyReady;
                    isEligible      = false;
                    reasons.Add("AutoPreparePayment is disabled. Settlement already ReadyForSettlement.");
                }
                else if (s.PayoutRecordId.HasValue)
                {
                    predictedAction = Actions.WouldSkipScheduled;
                    isEligible      = false;
                    reasons.Add("Payment is already prepared (PayoutRecordId is set).");
                }
                else
                {
                    predictedAction = Actions.WouldPreparePayment;
                    wouldPayment++;

                    if (autoPrepareInvoice && !s.InvoiceId.HasValue)
                    {
                        predictedAction = $"{predictedAction} → {Actions.WouldPrepareInvoice}";
                        wouldInvoice++;
                    }
                }
            }
            else
            {
                predictedAction = $"Skipped:{s.Status}";
                isEligible      = false;
                reasons.Add($"Status {s.Status} is not eligible for automation.");
            }

            items.Add(new CargoDrySettlementAutomationPreviewItemDto
            {
                SettlementId               = s.Id,
                SettlementCode             = s.SettlementCode,
                ProviderProfileId          = s.ProviderProfileId,
                ProductCode                = s.ProductCode,
                CurrencyCode               = s.CurrencyCode,
                PeriodYearMonth            = periodYearMonth,
                CurrentStatus              = s.Status,
                UnresolvedAttributionCount = unresolved,
                TotalAttributionCount      = allAttributions.Count,
                PredictedAction            = predictedAction,
                IsEligible                 = isEligible,
                IneligibilityReasons       = reasons,
            });
        }

        return new CargoDrySettlementAutomationPreviewDto
        {
            TargetYearMonth          = targetYearMonth,
            TotalSettlementsFound    = settlementsInScope.Count,
            TotalEligible            = items.Count(x => x.IsEligible),
            TotalIneligible          = items.Count(x => !x.IsEligible),
            TotalWouldMarkReady      = wouldReady,
            TotalWouldPreparePayment = wouldPayment,
            TotalWouldPrepareInvoice = wouldInvoice,
            AutoPreparePayment       = autoPreparePayment,
            AutoPrepareInvoice       = autoPrepareInvoice,
            Items                    = items,
        };
    }

    /// <inheritdoc/>
    public async Task<CargoDrySettlementAutomationRunDto> RunAsync(
        string                           runCode,
        int                              targetYearMonth,
        CargoDrySettlementAutomationMode mode,
        bool                             autoPreparePayment,
        bool                             autoPrepareInvoice,
        long                             triggeredByUserId,
        string?                          note,
        CancellationToken                ct)
    {
        var nowUtc = DateTime.UtcNow;

        // Create and persist the run entity (Pending → Running)
        var run = CargoDrySettlementAutomationRunEntity.Create(
            runCode:            runCode,
            targetYearMonth:    targetYearMonth,
            mode:               mode,
            autoPreparePayment: autoPreparePayment,
            autoPrepareInvoice: autoPrepareInvoice,
            triggeredByUserId:  triggeredByUserId,
            note:               note,
            nowUtc:             nowUtc);

        await _runs.AddAsync(run, ct);
        await _runs.SaveChangesAsync(ct);

        run.MarkRunning(DateTime.UtcNow);
        await _runs.SaveChangesAsync(ct);

        CargoDrySettlementAutomationRunEntity result;
        if (mode == CargoDrySettlementAutomationMode.DryRun)
        {
            // DryRun: collect preview data, produce run items, but make NO settlement mutations
            result = await ExecuteDryRunAsync(run, targetYearMonth, autoPreparePayment, autoPrepareInvoice, ct);
        }
        else
        {
            result = await ExecuteLiveRunAsync(run, targetYearMonth, autoPreparePayment, autoPrepareInvoice, triggeredByUserId, ct);
        }

        return MapToDto(result);
    }

    private static CargoDrySettlementAutomationRunDto MapToDto(CargoDrySettlementAutomationRunEntity e)
        => new()
        {
            Id                        = e.Id,
            RunCode                   = e.RunCode,
            TargetYearMonth           = e.TargetYearMonth,
            Mode                      = e.Mode,
            Status                    = e.Status,
            AutoCompletePayout        = e.AutoCompletePayout,
            AutoPreparePayment        = e.AutoPreparePayment,
            AutoPrepareInvoice        = e.AutoPrepareInvoice,
            TriggeredByUserId         = e.TriggeredByUserId,
            TriggeredAtUtc            = e.TriggeredAtUtc,
            CompletedAtUtc            = e.CompletedAtUtc,
            DurationMs                = e.DurationMs,
            TotalSettlementsFound     = e.TotalSettlementsFound,
            TotalSettlementsEligible  = e.TotalSettlementsEligible,
            TotalSettlementsProcessed = e.TotalSettlementsProcessed,
            TotalSettlementsSkipped   = e.TotalSettlementsSkipped,
            TotalSettlementsErrored   = e.TotalSettlementsErrored,
            Note                      = e.Note,
            ErrorSummary              = e.ErrorSummary,
            CreatedAtUtc              = e.CreatedAtUtc,
            RunItems = e.RunItems.Select(i => new CargoDrySettlementAutomationRunItemDto
            {
                Id                   = i.Id,
                RunId                = i.RunId,
                SettlementId         = i.SettlementId,
                SettlementCode       = i.SettlementCode,
                ProviderProfileId    = i.ProviderProfileId,
                ProductCode          = i.ProductCode,
                CurrencyCode         = i.CurrencyCode,
                PeriodYearMonth      = i.PeriodYearMonth,
                StatusBefore         = i.StatusBefore,
                Action               = i.Action,
                Success              = i.Success,
                ErrorMessage         = i.ErrorMessage,
                AttributionsResolved = i.AttributionsResolved,
                AttributionsSkipped  = i.AttributionsSkipped,
                AttributionsErrored  = i.AttributionsErrored,
                ProcessedAtUtc       = i.ProcessedAtUtc,
            }).ToList(),
        };

    // ── DryRun ────────────────────────────────────────────────────────────────────

    private async Task<CargoDrySettlementAutomationRunEntity> ExecuteDryRunAsync(
        CargoDrySettlementAutomationRunEntity run,
        int                                  targetYearMonth,
        bool                                 autoPreparePayment,
        bool                                 autoPrepareInvoice,
        CancellationToken                    ct)
    {
        try
        {
            var preview = await GetPreviewAsync(targetYearMonth, autoPreparePayment, autoPrepareInvoice, ct);
            var nowUtc  = DateTime.UtcNow;

            foreach (var item in preview.Items)
            {
                run.AddItem(CargoDrySettlementAutomationRunItemEntity.Create(
                    runId:                run.Id,
                    settlementId:         item.SettlementId,
                    settlementCode:       item.SettlementCode,
                    providerProfileId:    item.ProviderProfileId,
                    productCode:          item.ProductCode,
                    currencyCode:         item.CurrencyCode,
                    periodYearMonth:      item.PeriodYearMonth,
                    statusBefore:         item.CurrentStatus,
                    action:               item.PredictedAction,
                    success:              true,
                    errorMessage:         item.IneligibilityReasons.Any()
                                            ? string.Join("; ", item.IneligibilityReasons)
                                            : null,
                    attributionsResolved: 0,
                    attributionsSkipped:  item.TotalAttributionCount,
                    attributionsErrored:  0,
                    processedAtUtc:       nowUtc));
            }

            run.Complete(
                totalFound:      preview.TotalSettlementsFound,
                totalEligible:   preview.TotalEligible,
                totalProcessed:  0,
                totalSkipped:    preview.TotalIneligible,
                totalErrored:    0,
                errorSummary:    null,
                nowUtc:          DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DryRun failed for run {RunCode}", run.RunCode);
            run.Fail($"DryRun analysis failed: {ex.Message}", DateTime.UtcNow);
        }

        await _runs.SaveChangesAsync(ct);
        return run;
    }

    // ── Live Run ──────────────────────────────────────────────────────────────────

    private async Task<CargoDrySettlementAutomationRunEntity> ExecuteLiveRunAsync(
        CargoDrySettlementAutomationRunEntity run,
        int                                  targetYearMonth,
        bool                                 autoPreparePayment,
        bool                                 autoPrepareInvoice,
        long                                 triggeredByUserId,
        CancellationToken                    ct)
    {
        var settlementsInScope = new List<CargoDrySellThroughSettlementEntity>();

        try
        {
            settlementsInScope = await _settlements.GetForYearMonthAsync(
                targetYearMonth, EligibleStatuses, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settlements for run {RunCode}", run.RunCode);
            run.Fail($"Failed to load settlements: {ex.Message}", DateTime.UtcNow);
            await _runs.SaveChangesAsync(ct);
            return run;
        }

        var totalFound    = settlementsInScope.Count;
        var totalEligible = 0;
        var totalProcessed = 0;
        var totalSkipped   = 0;
        var totalErrored   = 0;
        var errorLines     = new List<string>();

        foreach (var settlement in settlementsInScope)
        {
            var nowUtc          = DateTime.UtcNow;
            var periodYearMonth = settlement.PeriodStartUtc.Year * 100 + settlement.PeriodStartUtc.Month;
            var statusBefore    = settlement.Status;

            var (action, success, errorMsg, resolved, skipped, errored) =
                await ProcessSettlementAsync(
                    settlement, autoPreparePayment, autoPrepareInvoice, triggeredByUserId, nowUtc, ct);

            run.AddItem(CargoDrySettlementAutomationRunItemEntity.Create(
                runId:                run.Id,
                settlementId:         settlement.Id,
                settlementCode:       settlement.SettlementCode,
                providerProfileId:    settlement.ProviderProfileId,
                productCode:          settlement.ProductCode,
                currencyCode:         settlement.CurrencyCode,
                periodYearMonth:      periodYearMonth,
                statusBefore:         statusBefore,
                action:               action,
                success:              success,
                errorMessage:         errorMsg,
                attributionsResolved: resolved,
                attributionsSkipped:  skipped,
                attributionsErrored:  errored,
                processedAtUtc:       DateTime.UtcNow));

            if (action.StartsWith("Skipped"))
                totalSkipped++;
            else if (!success)
            {
                totalErrored++;
                errorLines.Add($"{settlement.SettlementCode}: {errorMsg}");
            }
            else
            {
                totalEligible++;
                totalProcessed++;
            }
        }

        run.Complete(
            totalFound:      totalFound,
            totalEligible:   totalEligible,
            totalProcessed:  totalProcessed,
            totalSkipped:    totalSkipped,
            totalErrored:    totalErrored,
            errorSummary:    errorLines.Any() ? string.Join("\n", errorLines) : null,
            nowUtc:          DateTime.UtcNow);

        await _runs.SaveChangesAsync(ct);
        return run;
    }

    private async Task<(string action, bool success, string? errorMsg, int resolved, int skipped, int errored)>
        ProcessSettlementAsync(
            CargoDrySellThroughSettlementEntity settlement,
            bool                                autoPreparePayment,
            bool                                autoPrepareInvoice,
            long                                triggeredByUserId,
            DateTime                            nowUtc,
            CancellationToken                   ct)
    {
        try
        {
            // ── PENDING settlements ───────────────────────────────────────────────
            if (settlement.Status == CargoDrySellThroughSettlementStatus.Pending)
            {
                var unresolved = await _attributions.GetUnresolvedBySettlementIdAsync(settlement.Id, ct);
                if (unresolved.Count > 0)
                    return (Actions.SkippedUnresolved, true, null, 0, unresolved.Count, 0);

                // All attributions resolved → dispatch ResolveMonthlySellThroughSettlement
                var allAttributions = await _attributions.GetBySettlementIdAsync(settlement.Id, ct);

                await _sender.Send(new ResolveMonthlySellThroughSettlementCommand
                {
                    SettlementId     = settlement.Id,
                    ResolvedByUserId = triggeredByUserId,
                    ResolutionNote   = "Auto-resolved by monthly settlement automation (Phase 6).",
                }, ct);

                var action = Actions.MarkedReadyForSettlement;

                // If AutoPreparePayment and settlement has no payout yet → dispatch PrepareCargoDrySettlementPayment
                if (autoPreparePayment)
                {
                    await _sender.Send(new PrepareCargoDrySettlementPaymentCommand
                    {
                        SettlementId     = settlement.Id,
                        PreparedByUserId = triggeredByUserId,
                        PreparationNote  = "Auto-prepared by monthly settlement automation (Phase 6).",
                    }, ct);

                    action = $"{action} → {Actions.PaymentPrepared}";

                    if (autoPrepareInvoice)
                    {
                        await _sender.Send(new PrepareCargoDrySettlementInvoiceCommand
                        {
                            SettlementId     = settlement.Id,
                            PreparedByUserId = triggeredByUserId,
                            PreparationNote  = "Auto-prepared by monthly settlement automation (Phase 6).",
                        }, ct);

                        action = $"{action} → {Actions.InvoicePrepared}";
                    }
                }

                return (action, true, null, allAttributions.Count, 0, 0);
            }

            // ── READY FOR SETTLEMENT settlements ──────────────────────────────────
            if (settlement.Status == CargoDrySellThroughSettlementStatus.ReadyForSettlement)
            {
                if (!autoPreparePayment)
                    return (Actions.SkippedAutoPaymentDisabled, true, null, 0, 0, 0);

                if (settlement.PayoutRecordId.HasValue)
                    return (Actions.SkippedAlreadyScheduled, true, null, 0, 0, 0);

                await _sender.Send(new PrepareCargoDrySettlementPaymentCommand
                {
                    SettlementId     = settlement.Id,
                    PreparedByUserId = triggeredByUserId,
                    PreparationNote  = "Auto-prepared by monthly settlement automation (Phase 6).",
                }, ct);

                var action = Actions.PaymentPrepared;

                if (autoPrepareInvoice && !settlement.InvoiceId.HasValue)
                {
                    await _sender.Send(new PrepareCargoDrySettlementInvoiceCommand
                    {
                        SettlementId     = settlement.Id,
                        PreparedByUserId = triggeredByUserId,
                        PreparationNote  = "Auto-prepared by monthly settlement automation (Phase 6).",
                    }, ct);

                    action = $"{action} → {Actions.InvoicePrepared}";
                }

                return (action, true, null, 0, 0, 0);
            }

            // Ineligible status — should not reach here given EligibleStatuses filter, but guard anyway
            return ($"Skipped:{settlement.Status}", true, null, 0, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing settlement {SettlementCode} in automation run",
                settlement.SettlementCode);

            return ($"{Actions.ErrorPrefix}{ex.GetType().Name}", false, ex.Message, 0, 0, 1);
        }
    }
}
