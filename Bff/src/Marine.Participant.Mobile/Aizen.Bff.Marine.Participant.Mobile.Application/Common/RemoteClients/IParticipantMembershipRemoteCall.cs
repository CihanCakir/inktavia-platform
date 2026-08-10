using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

/// <summary>
/// BFF → Payment module owner participant-membership endpoints (BE_MO7, <c>api/v1/payment/participant</c>). The
/// participant identity is asserted via the shared BFF-assertion path (the delegating handler sends the resolved
/// participant profile id in X-Aizen-Provider-Profile-Id, which the Payment owner controller reads) — the profile id
/// is NEVER in the body, and the price is resolved from the plan server-side. Plans deserialize straight into the
/// cost-free mobile DTO (Refit maps the matching fields, drops the rest). The subscription/subscribe/cancel/status
/// results reuse the Payment.Abstraction result records.
/// </summary>
public interface IParticipantMembershipRemoteCall : IAizenRemoteCall
{
    // Active participant plans (cost-free — Refit maps ParticipantPlanDto's customer fields into the mobile DTO).
    [AizenRemoteCallGet("/api/v1/payment/participant/plans")]
    Task<AizenApiResponse<List<MobileMembershipPlanDto>>> GetPlans();

    // The caller's current active subscription (null body when none).
    [AizenRemoteCallGet("/api/v1/payment/participant/subscription")]
    Task<AizenApiResponse<ActiveParticipantSubscriptionResult>> GetSubscription();

    // Subscribe — free applied now / paid → PendingIntent checkout. Body carries only the plan id (price server-side).
    [AizenRemoteCallPost("/api/v1/payment/participant/subscription")]
    Task<AizenApiResponse<SubscribeParticipantForOwnerResult>> Subscribe(
        [AizenRemoteCallBody] SubscribeParticipantForOwnerRequest request);

    // Cancel the active subscription (optional free-text reason).
    [AizenRemoteCallDelete("/api/v1/payment/participant/subscription")]
    Task<AizenApiResponse<CancelSubscriptionResult>> Cancel([Refit.Query] string? reason);

    // The paid-checkout payment status (owner-scoped module-side). Poll: Pending → Paid/Failed.
    [AizenRemoteCallGet("/api/v1/payment/participant/subscription/payment-status/{transactionId}")]
    Task<AizenApiResponse<ParticipantSubscriptionPaymentStatusResult>> GetPaymentStatus(long transactionId);
}
