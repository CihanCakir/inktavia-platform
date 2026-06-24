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

[DocumentationInfo("ServiceRequest admin BFF remote call", "Defines synchronous BFF-to-ServiceRequest calls for admin oversight and dispute management.")]
public interface IServiceRequestAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/admin/service-requests")]
    Task<AizenApiResponse<GetAdminServiceRequestListResponse>> GetAdminServiceRequestList(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] string? status = null,
        [Refit.Query] long? vesselId = null,
        [Refit.Query] long? ownerUserId = null,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/admin/service-requests/disputes")]
    Task<AizenApiResponse<GetAdminDisputeListResponse>> GetAdminDisputeList(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] string? status = null,
        [Refit.Query] int pageIndex = 0,
        [Refit.Query] int pageSize = 20);

    [AizenRemoteCallGet("/api/v1/admin/service-requests/{serviceRequestId}")]
    Task<AizenApiResponse<GetServiceRequestDetailResponse>> GetAdminServiceRequestDetail(
        long serviceRequestId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/cancel")]
    Task<AizenApiResponse<CancelServiceRequestResponse>> CancelServiceRequest(
        long serviceRequestId,
        [AizenRemoteCallBody] CancelServiceRequestRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/completion/approve")]
    Task<AizenApiResponse<ApproveServiceRequestCompletionResponse>> ApproveCompletion(
        long serviceRequestId,
        [AizenRemoteCallBody] ApproveServiceRequestCompletionRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/completion/reject")]
    Task<AizenApiResponse<RejectServiceRequestCompletionResponse>> RejectCompletion(
        long serviceRequestId,
        [AizenRemoteCallBody] RejectServiceRequestCompletionRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/dispute/{disputeId}/status")]
    Task<AizenApiResponse<EmptyResult>> ChangeDisputeStatus(
        long serviceRequestId,
        long disputeId,
        [AizenRemoteCallBody] ChangeServiceRequestDisputeStatusRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/service-requests/{serviceRequestId}/dispute/{disputeId}/resolve")]
    Task<AizenApiResponse<ResolveServiceRequestDisputeResponse>> ResolveDispute(
        long serviceRequestId,
        long disputeId,
        [AizenRemoteCallBody] ResolveServiceRequestDisputeRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPatch("/api/v1/admin/service-requests/{serviceRequestId}/status")]
    Task<AizenApiResponse<UpdateServiceRequestStatusResponse>> UpdateAdminServiceRequestStatus(
        long serviceRequestId,
        [AizenRemoteCallBody] UpdateServiceRequestStatusRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/assign")]
    Task<AizenApiResponse<AssignProviderResponse>> AssignServiceRequestProvider(
        long serviceRequestId,
        [AizenRemoteCallBody] AssignProviderRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/complete")]
    Task<AizenApiResponse<CompleteServiceRequestResponse>> CompleteServiceRequest(
        long serviceRequestId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/dispute")]
    Task<AizenApiResponse<DisputeServiceRequestResponse>> DisputeServiceRequest(
        long serviceRequestId,
        [AizenRemoteCallBody] DisputeServiceRequestRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/admin/service-requests/{serviceRequestId}/offers")]
    Task<AizenApiResponse<GetProviderOffersResponse>> GetAdminServiceRequestOffers(
        long serviceRequestId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/offers/{offerId}/accept")]
    Task<AizenApiResponse<AcceptServiceRequestOfferResponse>> AcceptServiceRequestOffer(
        long serviceRequestId,
        long offerId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/offers/{offerId}/reject")]
    Task<AizenApiResponse<RejectServiceRequestOfferResponse>> RejectServiceRequestOffer(
        long serviceRequestId,
        long offerId,
        [AizenRemoteCallBody] RejectServiceRequestOfferRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/admin/service-requests/{serviceRequestId}/work-logs")]
    Task<AizenApiResponse<GetWorkLogsResponse>> GetAdminWorkLogs(
        long serviceRequestId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/work-logs")]
    Task<AizenApiResponse<AddWorkLogEntryResponse>> AddAdminWorkLogEntry(
        long serviceRequestId,
        [AizenRemoteCallBody] AddWorkLogEntryRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/service-requests/{serviceRequestId}/payment/release")]
    Task<AizenApiResponse<ReleasePaymentResponse>> ReleaseServiceRequestPayment(
        long serviceRequestId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/messages/conversations")]
    Task<AizenApiResponse<GetConversationListResponse>> GetAdminConversations(
        [Refit.Query] string? filter,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/messages/conversations/{conversationId}")]
    Task<AizenApiResponse<GetConversationDetailResponse>> GetAdminConversationDetail(
        long conversationId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);
}

