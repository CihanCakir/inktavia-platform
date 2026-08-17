using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.Mapping;

/// <summary>
/// N-E — the single place that maps a structured ServiceRequest reject/cancel reason to a Payment
/// <see cref="RefundReason"/>. The mapped reason rides the cancel event so the Payment refund flow
/// (RefundCauseMap → RefundAllocationPolicy, P10) picks the correct allocation deterministically —
/// no free-text guessing. Keep the free-text note on the entity for audit / dispute (S13).
/// </summary>
public static class ServiceRequestReasonRefundMap
{
    /// <summary>Owner cancels the service request.</summary>
    public static RefundReason ToRefundReason(ServiceRequestCancelReason? reason) => reason switch
    {
        ServiceRequestCancelReason.NoLongerNeeded       => RefundReason.UserCancel,
        ServiceRequestCancelReason.FoundAnotherProvider => RefundReason.UserCancel,
        ServiceRequestCancelReason.PriceTooHigh         => RefundReason.UserCancel,
        ServiceRequestCancelReason.ChangedMind          => RefundReason.UserCancel,
        ServiceRequestCancelReason.ProviderUnresponsive => RefundReason.ProviderFailedToDeliver,
        ServiceRequestCancelReason.Duplicate            => RefundReason.DuplicateCharge,
        // Other / null → the generic SR-cancel cause (RefundCauseMap → CustomerCancelledBeforeWork).
        _                                               => RefundReason.ServiceRequestCancelled,
    };

    /// <summary>Provider rejects an assignment (provider-fault refund cause).</summary>
    public static RefundReason ToRefundReason(AssignmentRejectReason? reason) => reason switch
    {
        // Every provider-side reject is a provider failure to deliver.
        _ => RefundReason.ProviderFailedToDeliver,
    };

    /// <summary>Owner rejects a submitted completion (service-not-delivered cause).</summary>
    public static RefundReason ToRefundReason(CompletionRejectReason? reason) => reason switch
    {
        CompletionRejectReason.WorkIncomplete => RefundReason.ServiceNotDelivered,
        CompletionRejectReason.QualityIssue   => RefundReason.ServiceNotDelivered,
        CompletionRejectReason.NotAsAgreed    => RefundReason.ServiceNotDelivered,
        _                                     => RefundReason.ServiceNotDelivered,
    };

    /// <summary>Owner rejects an offer (pre-payment; provided for dispute/audit completeness).</summary>
    public static RefundReason ToRefundReason(OfferRejectReason? reason) => reason switch
    {
        _ => RefundReason.UserCancel,
    };
}
