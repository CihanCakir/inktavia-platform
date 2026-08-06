using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("ServiceRequest admin BFF remote call",
    "Defines synchronous BFF-to-ServiceRequest calls for admin oversight and dispute management. " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IServiceRequestRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/admin/service-requests")]
    Task<AizenApiResponse<GetAdminServiceRequestListResponse>> GetAdminServiceRequestList(
        [Refit.Query] string? status            = null,
        [Refit.Query] long?   vesselId          = null,
        [Refit.Query] long?   ownerUserId       = null,
        [Refit.Query] int     pageIndex         = 0,
        [Refit.Query] int     pageSize          = 20,
        [Refit.Query] long?   providerProfileId = null);

    [AizenRemoteCallGet("/api/v1/admin/service-requests/disputes")]
    Task<AizenApiResponse<GetAdminDisputeListResponse>> GetAdminDisputeList(
        [Refit.Query] string? status    = null,
        [Refit.Query] int     pageIndex = 0,
        [Refit.Query] int     pageSize  = 20);

    [AizenRemoteCallGet("/api/v1/admin/service-requests/{serviceRequestId}")]
    Task<AizenApiResponse<GetServiceRequestDetailResponse>> GetAdminServiceRequestDetail(
        long serviceRequestId);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/cancel")]
    Task<AizenApiResponse<CancelServiceRequestResponse>> CancelServiceRequest(
        long serviceRequestId,
        [AizenRemoteCallBody] CancelServiceRequestRequest request);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/completion/approve")]
    Task<AizenApiResponse<ApproveServiceRequestCompletionResponse>> ApproveCompletion(
        long serviceRequestId,
        [AizenRemoteCallBody] ApproveServiceRequestCompletionRequest request);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/completion/reject")]
    Task<AizenApiResponse<RejectServiceRequestCompletionResponse>> RejectCompletion(
        long serviceRequestId,
        [AizenRemoteCallBody] RejectServiceRequestCompletionRequest request);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/dispute/{disputeId}/status")]
    Task<AizenApiResponse<EmptyResult>> ChangeDisputeStatus(
        long serviceRequestId,
        long disputeId,
        [AizenRemoteCallBody] ChangeServiceRequestDisputeStatusRequest request);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/dispute/{disputeId}/resolve")]
    Task<AizenApiResponse<ResolveServiceRequestDisputeResponse>> ResolveDispute(
        long serviceRequestId,
        long disputeId,
        [AizenRemoteCallBody] ResolveServiceRequestDisputeRequest request);

    [AizenRemoteCallPatch("/api/v1/admin/service-requests/{serviceRequestId}/status")]
    Task<AizenApiResponse<UpdateServiceRequestStatusResponse>> UpdateAdminServiceRequestStatus(
        long serviceRequestId,
        [AizenRemoteCallBody] UpdateServiceRequestStatusRequest request);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/assign")]
    Task<AizenApiResponse<AssignProviderResponse>> AssignServiceRequestProvider(
        long serviceRequestId,
        [AizenRemoteCallBody] AssignProviderRequest request);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/complete")]
    Task<AizenApiResponse<CompleteServiceRequestResponse>> CompleteServiceRequest(
        long serviceRequestId);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/dispute")]
    Task<AizenApiResponse<DisputeServiceRequestResponse>> DisputeServiceRequest(
        long serviceRequestId,
        [AizenRemoteCallBody] DisputeServiceRequestRequest request);

    [AizenRemoteCallGet("/api/v1/admin/service-requests/{serviceRequestId}/offers")]
    Task<AizenApiResponse<GetProviderOffersResponse>> GetAdminServiceRequestOffers(
        long serviceRequestId);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/offers/{offerId}/accept")]
    Task<AizenApiResponse<AcceptServiceRequestOfferResponse>> AcceptServiceRequestOffer(
        long serviceRequestId,
        long offerId);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/offers/{offerId}/reject")]
    Task<AizenApiResponse<RejectServiceRequestOfferResponse>> RejectServiceRequestOffer(
        long serviceRequestId,
        long offerId,
        [AizenRemoteCallBody] RejectServiceRequestOfferRequest request);

    [AizenRemoteCallGet("/api/v1/admin/service-requests/{serviceRequestId}/work-logs")]
    Task<AizenApiResponse<GetWorkLogsResponse>> GetAdminWorkLogs(long serviceRequestId);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/work-logs")]
    Task<AizenApiResponse<AddWorkLogEntryResponse>> AddAdminWorkLogEntry(
        long serviceRequestId,
        [AizenRemoteCallBody] AddWorkLogEntryRequest request);

    [AizenRemoteCallPatch("/api/v1/admin/service-requests/{serviceRequestId}/work-logs/phases/{phaseNumber}")]
    Task<AizenApiResponse<UpdateWorkPhaseResponse>> UpdateAdminWorkPhase(
        long serviceRequestId,
        int phaseNumber,
        [AizenRemoteCallBody] UpdateWorkPhaseRequest request);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/payment/release")]
    Task<AizenApiResponse<ReleasePaymentResponse>> ReleaseServiceRequestPayment(
        long serviceRequestId);

    // --- S2 pricing attribute definitions (admin CRUD) ---
    [AizenRemoteCallGet("/api/v1/admin/service-requests/pricing-attributes")]
    Task<AizenApiResponse<List<Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing.PricingAttributeDefinitionDto>>> GetAdminPricingAttributes(
        [Refit.Query] string? serviceCategoryCode = null);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/pricing-attributes")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing.PricingAttributeDefinitionDto>> CreateAdminPricingAttribute(
        [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing.PricingAttributeDefinitionRequest request);

    [AizenRemoteCallPut("/api/v1/admin/service-requests/pricing-attributes/{id}")]
    Task<AizenApiResponse<Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing.PricingAttributeDefinitionDto>> UpdateAdminPricingAttribute(
        long id, [AizenRemoteCallBody] Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing.PricingAttributeDefinitionRequest request);

    // Module DELETE deactivates (keeps the code) and returns an ad-hoc { header:{ isSuccess } } — bind as a plain Task.
    [AizenRemoteCallDelete("/api/v1/admin/service-requests/pricing-attributes/{id}")]
    Task DeleteAdminPricingAttribute(long id);

    [AizenRemoteCallGet("/api/v1/messages/conversations")]
    Task<AizenApiResponse<GetConversationListResponse>> GetAdminConversations(
        [Refit.Query] string? filter);

    [AizenRemoteCallGet("/api/v1/messages/conversations/{conversationId}")]
    Task<AizenApiResponse<GetConversationDetailResponse>> GetAdminConversationDetail(
        long conversationId);
}
