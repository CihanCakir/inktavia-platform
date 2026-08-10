using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

/// <summary>
/// BFF → ServiceRequest module owner endpoints (BE_MO1). The caller is asserted as the participant via the
/// X-Aizen-Bff-Assertion service-token path (the delegating handler sets X-Aizen-User-Id from the resolved
/// identity holder), so every owner endpoint resolves ownership module-side from UserInfo.UserId — the owner id
/// is NEVER taken from the request body/query. Verbs mirror the module contract exactly (publish + cancel are
/// PATCH). Detail carries no module-side ownership check, so the BFF gates it against the caller's own owner id.
/// </summary>
public interface IServiceRequestRemoteCall : IAizenRemoteCall
{
    // Create a Draft service request (owner = asserted caller; OwnerUserId is resolved module-side).
    [AizenRemoteCallPost("/api/v1/service-requests")]
    Task<AizenApiResponse<CreateServiceRequestResponse>> Create(
        [AizenRemoteCallBody] CreateServiceRequestRequest request);

    // Publish a Draft (Draft → Open). Module transitions the status + records history.
    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/publish")]
    Task<AizenApiResponse<UpdateServiceRequestResponse>> Publish(long serviceRequestId);

    // The caller's own requests (owner scoped module-side). Filter binds as query params.
    [AizenRemoteCallGet("/api/v1/service-requests/my")]
    Task<AizenApiResponse<GetServiceRequestListResponse>> GetMy(
        [Refit.Query] ServiceRequestListFilterRequest filter);

    // Full detail by id (module does NOT owner-gate — the BFF gates against the caller's own owner id).
    [AizenRemoteCallGet("/api/v1/service-requests/{serviceRequestId}")]
    Task<AizenApiResponse<GetServiceRequestDetailResponse>> GetDetail(long serviceRequestId);

    // Update a Draft's core fields (owner-gated module-side).
    [AizenRemoteCallPut("/api/v1/service-requests/{serviceRequestId}")]
    Task<AizenApiResponse<UpdateServiceRequestResponse>> Update(
        long serviceRequestId, [AizenRemoteCallBody] UpdateServiceRequestRequest request);

    // Cancel with the N-E structured reason + optional note (owner-gated module-side).
    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/cancel")]
    Task<AizenApiResponse<CancelServiceRequestResponse>> Cancel(
        long serviceRequestId, [AizenRemoteCallBody] CancelServiceRequestRequest request);

    // Attach an already-uploaded file (client-side presigned, then completed) as a typed attachment.
    [AizenRemoteCallPost("/api/v1/service-requests/{serviceRequestId}/attachments")]
    Task<AizenApiResponse<AddServiceRequestAttachmentResponse>> AddAttachment(
        long serviceRequestId, [AizenRemoteCallBody] AddServiceRequestAttachmentRequest request);

    // ── BE_MO2 — owner offers inbox (cost-free) ─────────────────────────────────────────────────────────
    // Offers received on the owner's SR (module owner-gates via UserInfo.UserId). Excludes Drafts.
    [AizenRemoteCallGet("/api/v1/service-requests/{serviceRequestId}/offers/received")]
    Task<AizenApiResponse<GetServiceRequestOffersForOwnerResponse>> GetOwnerOffers(long serviceRequestId);

    // One received offer with its full cost-free breakdown (module owner-gates).
    [AizenRemoteCallGet("/api/v1/service-requests/{serviceRequestId}/offers/received/{offerId}")]
    Task<AizenApiResponse<GetServiceRequestOfferForOwnerResponse>> GetOwnerOffer(long serviceRequestId, long offerId);

    // Reject a received offer with the N-E structured reason + optional note. The module reject does NOT owner-gate,
    // so the BFF gates ownership (EnsureOwnedAsync) before proxying.
    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/offers/{offerId}/reject")]
    Task<AizenApiResponse<RejectServiceRequestOfferResponse>> RejectOwnerOffer(
        long serviceRequestId, long offerId, [AizenRemoteCallBody] RejectServiceRequestOfferRequest request);

    // ── BE_MO3 — owner accept + pay + status ────────────────────────────────────────────────────────────
    // Accept a received offer → module runs BE-P8 economics + escrow (capture-at-accept) BEFORE committing; on
    // Rejected/ConfigError it throws (no half-accept). The module accept does NOT owner-gate, so the BFF gates
    // ownership (EnsureOwnedAsync) before proxying. Amounts are server-owned; the body carries only the offerId.
    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/offers/{offerId}/accept")]
    Task<AizenApiResponse<AcceptServiceRequestOfferResponse>> AcceptOwnerOffer(
        long serviceRequestId, long offerId, [AizenRemoteCallBody] AcceptServiceRequestOfferRequest request);

    // Owner-facing payment status of the accepted SR (poll: Pending → Paid/Failed). Module owner-gates via
    // UserInfo.UserId; the BFF also gates ownership before proxying. Cost-free (customer total + status only).
    [AizenRemoteCallGet("/api/v1/service-requests/{serviceRequestId}/payment-status")]
    Task<AizenApiResponse<GetServiceRequestPaymentStatusForOwnerResponse>> GetOwnerPaymentStatus(long serviceRequestId);

    // ── BE_MO4 — owner completion review (reuses approve/reject + the decoupled escrow release) ─────────────
    // Approve the provider's completion (optional owner rating) → SR Completed → the existing decoupled escrow
    // release runs (untouched). The module approve does NOT owner-gate, so the BFF gates ownership before proxying.
    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/completion/approve")]
    Task<AizenApiResponse<ApproveServiceRequestCompletionResponse>> ApproveOwnerCompletion(
        long serviceRequestId, [AizenRemoteCallBody] ApproveServiceRequestCompletionRequest request);

    // Reject the provider's completion with the N-E structured reason + optional note (SR → InProgress; no money).
    // The module reject does NOT owner-gate, so the BFF gates ownership before proxying.
    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/completion/reject")]
    Task<AizenApiResponse<RejectServiceRequestCompletionResponse>> RejectOwnerCompletion(
        long serviceRequestId, [AizenRemoteCallBody] RejectServiceRequestCompletionRequest request);

    // ── BE_MO5 — owner disputes (open + my-disputes list + read-only cost-free case) ────────────────────────
    // Open a dispute (N-E reason + description). The shared module endpoint resolves the actor from roles → Owner
    // for an asserted participant. It does NOT owner-gate the SR, so the BFF gates ownership (EnsureOwnedAsync)
    // before proxying. Resolution / status-change stay Admin-only — never proxied here.
    [AizenRemoteCallPost("/api/v1/service-requests/{serviceRequestId}/dispute")]
    Task<AizenApiResponse<OpenServiceRequestDisputeResponse>> OpenDispute(
        long serviceRequestId, [AizenRemoteCallBody] OpenServiceRequestDisputeRequest request);

    // The caller-owner's own disputes + a global open/actionable count (owner scoped module-side via UserInfo.UserId).
    [AizenRemoteCallGet("/api/v1/service-requests/owner/disputes")]
    Task<AizenApiResponse<GetOwnerDisputesResponse>> GetOwnerDisputes(
        [Refit.Query] int pageIndex, [Refit.Query] int pageSize, [Refit.Query] ServiceRequestDisputeStatus? status);

    // The cost-free dispute case for the owner (read-only). The owner endpoint gates by SR ownership module-side;
    // the BFF ALSO owner-gates via EnsureOwnedAsync before calling (defense-in-depth).
    [AizenRemoteCallGet("/api/v1/service-requests/owner/disputes/{disputeId}/case")]
    Task<AizenApiResponse<GetDisputeCaseDetailResponse>> GetOwnerDisputeCase(long disputeId);
}
