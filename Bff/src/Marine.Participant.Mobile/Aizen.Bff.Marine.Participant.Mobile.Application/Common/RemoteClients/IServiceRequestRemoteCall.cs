using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
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
    // so the BFF gates ownership (EnsureOwnedAsync) before proxying. Accept is deliberately NOT wired (MO3).
    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/offers/{offerId}/reject")]
    Task<AizenApiResponse<RejectServiceRequestOfferResponse>> RejectOwnerOffer(
        long serviceRequestId, long offerId, [AizenRemoteCallBody] RejectServiceRequestOfferRequest request);
}
