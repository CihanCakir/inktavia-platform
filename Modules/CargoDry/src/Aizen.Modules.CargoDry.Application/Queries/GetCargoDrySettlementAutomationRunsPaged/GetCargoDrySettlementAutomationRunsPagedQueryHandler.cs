using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementAutomationRunsPaged;

[DocumentationInfo("Get CargoDry settlement automation runs paged query handler",
    "Queries ICargoDrySettlementAutomationRunRepository.GetPagedAsync and maps to DTOs. " +
    "Run items are not loaded for list queries. Phase 6 (July 2026).")]
public sealed class GetCargoDrySettlementAutomationRunsPagedQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementAutomationRunsPagedQuery,
                        GetCargoDrySettlementAutomationRunsPagedQueryResponse>
{
    private readonly ICargoDrySettlementAutomationRunRepository _runs;

    public GetCargoDrySettlementAutomationRunsPagedQueryHandler(
        ICargoDrySettlementAutomationRunRepository runs)
        => _runs = runs;

    public override async Task<GetCargoDrySettlementAutomationRunsPagedQueryResponse> Handle(
        GetCargoDrySettlementAutomationRunsPagedQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _runs.GetPagedAsync(
            targetYearMonth:    request.TargetYearMonth,
            status:             request.Status,
            mode:               request.Mode,
            triggeredByUserId:  request.TriggeredByUserId,
            fromUtc:            request.FromUtc,
            toUtc:              request.ToUtc,
            skip:               skip,
            take:               request.PageSize,
            ct:                 ct);

        return new GetCargoDrySettlementAutomationRunsPagedQueryResponse
        {
            Items    = items.Select(MapToDto).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
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
            RunItems                  = [], // not loaded for list queries
        };
}
