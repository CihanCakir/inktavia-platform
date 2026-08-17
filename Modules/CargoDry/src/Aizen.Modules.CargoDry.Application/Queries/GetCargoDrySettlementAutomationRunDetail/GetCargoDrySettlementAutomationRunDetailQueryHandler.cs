using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementAutomationRunDetail;

[DocumentationInfo("Get CargoDry settlement automation run detail query handler",
    "Loads the run entity with all items included via ICargoDrySettlementAutomationRunRepository.GetByIdAsync. " +
    "Throws AizenBusinessException if not found. Phase 6 (July 2026).")]
public sealed class GetCargoDrySettlementAutomationRunDetailQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementAutomationRunDetailQuery,
                        GetCargoDrySettlementAutomationRunDetailQueryResponse>
{
    private readonly ICargoDrySettlementAutomationRunRepository _runs;

    public GetCargoDrySettlementAutomationRunDetailQueryHandler(
        ICargoDrySettlementAutomationRunRepository runs)
        => _runs = runs;

    public override async Task<GetCargoDrySettlementAutomationRunDetailQueryResponse> Handle(
        GetCargoDrySettlementAutomationRunDetailQuery request, CancellationToken ct)
    {
        var run = await _runs.GetByIdAsync(request.RunId, ct)
            ?? throw new AizenBusinessException(
                $"Settlement automation run with id {request.RunId} was not found.");

        return new GetCargoDrySettlementAutomationRunDetailQueryResponse { Run = MapToDto(run) };
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
}
