using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySellThroughSettlementDetail;

[DocumentationInfo("Get CargoDry sell-through settlement detail query handler",
    "Returns the full detail of a single sell-through settlement record by Id. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class GetCargoDrySellThroughSettlementDetailQueryHandler
    : AizenQueryHandler<GetCargoDrySellThroughSettlementDetailQuery, GetCargoDrySellThroughSettlementDetailResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;

    public GetCargoDrySellThroughSettlementDetailQueryHandler(
        ICargoDrySellThroughSettlementRepository settlements)
        => _settlements = settlements;

    public override async Task<GetCargoDrySellThroughSettlementDetailResponse> Handle(
        GetCargoDrySellThroughSettlementDetailQuery request, CancellationToken ct)
    {
        var x = await _settlements.GetByIdAsync(request.Id, ct);
        if (x is null)
            return new GetCargoDrySellThroughSettlementDetailResponse { Detail = null };

        return new GetCargoDrySellThroughSettlementDetailResponse
        {
            Detail = new CargoDrySellThroughSettlementDto
            {
                Id                      = x.Id,
                PublicId                = x.PublicId,
                SettlementCode          = x.SettlementCode,
                ConsignmentAgreementId  = x.ConsignmentAgreementId,
                ProviderProfileId       = x.ProviderProfileId,
                ProductCode             = x.ProductCode,
                BatchCode               = x.BatchCode,
                TotalKitCount           = x.TotalKitCount,
                SettledKitCount         = x.SettledKitCount,
                TotalSaleAmount         = x.TotalSaleAmount,
                TotalCommissionAmount   = x.TotalCommissionAmount,
                ProviderPayoutAmount    = x.ProviderPayoutAmount,
                CurrencyCode            = x.CurrencyCode,
                PeriodStartUtc          = x.PeriodStartUtc,
                PeriodEndUtc            = x.PeriodEndUtc,
                Status                  = x.Status,
                StatusName              = x.Status.ToString(),
                ScheduledSettlementDate = x.ScheduledSettlementDate,
                SettledAtUtc            = x.SettledAtUtc,
                SettledByUserId         = x.SettledByUserId,
                DisputeReason           = x.DisputeReason,
                Note                    = x.Note,
                ReadyForSettlementAtUtc = x.ReadyForSettlementAtUtc,
                // Phase 4B
                PayoutRecordId          = x.PayoutRecordId,
                PaymentPreparedAtUtc    = x.PaymentPreparedAtUtc,
                PaymentPreparedByUserId = x.PaymentPreparedByUserId,
                PaymentPreparationNote  = x.PaymentPreparationNote,
                // Phase 4C
                InvoiceId               = x.InvoiceId,
                InvoicePreparedAtUtc    = x.InvoicePreparedAtUtc,
                InvoicePreparedByUserId = x.InvoicePreparedByUserId,
                InvoicePreparationNote  = x.InvoicePreparationNote,
                // Phase 4D
                PayoutCompletedAtUtc      = x.PayoutCompletedAtUtc,
                PayoutCompletedByUserId   = x.PayoutCompletedByUserId,
                PayoutCompletionReference = x.PayoutCompletionReference,
                PayoutFailureReason       = x.PayoutFailureReason,
                PayoutLifecycleNote       = x.PayoutLifecycleNote,
                CreatedAtUtc            = x.CreatedAtUtc,
            }
        };
    }
}
