using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;

namespace Aizen.Modules.ServiceRequest.Application.Mapping;

/// <summary>
/// BE-S13c — builds the <see cref="ServiceRequestDisputeResolvedMessage"/> from the resolved dispute + both party ids.
/// Pure so N3's payload contract (both parties + outcome + refund amount) is unit-testable. <c>Outcome</c> is 0 and
/// <c>RefundAmount</c> is 0 for a notes-only resolve (no monetary outcome applied).
/// </summary>
public static class DisputeResolvedMessageFactory
{
    public static ServiceRequestDisputeResolvedMessage Build(
        ServiceRequestDisputeEntity dispute, long ownerUserId, long providerUserId) => new()
    {
        ServiceRequestId      = dispute.ServiceRequestId,
        DisputeId             = dispute.Id,
        ResolvedByAdminUserId = dispute.ResolvedByAdminUserId ?? 0,
        OwnerUserId           = ownerUserId,
        ProviderUserId        = providerUserId,
        Reason                = dispute.Reason,
        Outcome               = (int)(dispute.ResolutionOutcome ?? 0),
        RefundAmount          = dispute.ResolutionRefundAmount ?? 0m,
    };
}
