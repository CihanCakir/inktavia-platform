using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;
using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;

namespace Aizen.Modules.ServiceRequest.Application.Mapping;

/// <summary>BE-S11b — projects change-order aggregates to DTOs (no economics math; the applied amounts are already stored).</summary>
public static class ServiceChangeOrderMapper
{
    public static ServiceChangeOrderDto ToDto(this ServiceChangeOrderEntity co) => new()
    {
        Id                   = co.Id,
        ServiceRequestId     = co.ServiceRequestId,
        AcceptedOfferId      = co.AcceptedOfferId,
        SequenceNo           = co.SequenceNo,
        Status               = co.Status,
        Direction            = co.Direction,
        CurrencyCode         = co.CurrencyCode,
        Reason               = co.Reason,
        RejectionReason      = co.RejectionReason,
        ProposedByUserId     = co.ProposedByUserId,
        AppliedCustomerTotal = co.AppliedCustomerTotal,
        AppliedProviderNet   = co.AppliedProviderNet,
        EffectiveTotalDelta  = co.EffectiveTotalDelta,
        EconomicsSnapshotId  = co.EconomicsSnapshotId,
        PaymentTransactionId = co.PaymentTransactionId,
        RefundRecordId       = co.RefundRecordId,
        ProposedAt           = co.ProposedAt,
        CustomerApprovedAt   = co.CustomerApprovedAt,
        RejectedAt           = co.RejectedAt,
        AppliedAt            = co.AppliedAt,
        CancelledAt          = co.CancelledAt,
        Items                = co.Items.OrderBy(i => i.SortOrder).Select(i => new ServiceChangeOrderItemDto
        {
            Id                     = i.Id,
            ItemType               = i.ItemType,
            Title                  = i.Title,
            Description            = i.Description,
            Quantity               = i.Quantity,
            UnitPrice              = i.UnitPrice,
            CurrencyCode           = i.CurrencyCode,
            SortOrder              = i.SortOrder,
            UnitCode               = i.UnitCode,
            TaxRate                = i.TaxRate,
            PricingMethod          = i.PricingMethod,
            CommissionEligibility  = i.CommissionEligibility,
            LineDiscountEligibility = i.LineDiscountEligibility,
        }).ToList(),
    };
}
