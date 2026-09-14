using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ChangeOrder;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Maintenance;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests.CargoDry;

/// <summary>
/// Test double for the mobile BFF → ServiceRequest remote call. Only NotifyCargoDryKitActivated is functional (the
/// activate handler calls it best-effort); the rest of the owner surface throws (unused by these tests).
/// </summary>
internal sealed class FakeServiceRequestRemoteCall : IServiceRequestRemoteCall
{
    public int NotifyCount { get; private set; }
    public CargoDryKitActivatedRequest? LastNotifyBody { get; private set; }
    public Exception? ThrowOnNotify { get; set; }

    public Task<AizenApiResponse<CompleteCargoDrySupplyOnActivationResponse>> NotifyCargoDryKitActivated(
        CargoDryKitActivatedRequest request)
    {
        NotifyCount++;
        LastNotifyBody = request;
        if (ThrowOnNotify is not null) throw ThrowOnNotify;
        return Task.FromResult(new AizenApiResponse<CompleteCargoDrySupplyOnActivationResponse>(
            AizenResponseHeader.Success(),
            new CompleteCargoDrySupplyOnActivationResponse { Correlated = false }));
    }

    public Task<AizenApiResponse<CreateServiceRequestResponse>> Create(CreateServiceRequestRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<UpdateServiceRequestResponse>> Publish(long serviceRequestId) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetServiceRequestListResponse>> GetMy(ServiceRequestListFilterRequest filter) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetServiceRequestDetailResponse>> GetDetail(long serviceRequestId) => throw new NotImplementedException();
    public Task<AizenApiResponse<UpdateServiceRequestResponse>> Update(long serviceRequestId, UpdateServiceRequestRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<CancelServiceRequestResponse>> Cancel(long serviceRequestId, CancelServiceRequestRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<AddServiceRequestAttachmentResponse>> AddAttachment(long serviceRequestId, AddServiceRequestAttachmentRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetServiceRequestOffersForOwnerResponse>> GetOwnerOffers(long serviceRequestId) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetServiceRequestOfferForOwnerResponse>> GetOwnerOffer(long serviceRequestId, long offerId) => throw new NotImplementedException();
    public Task<AizenApiResponse<RejectServiceRequestOfferResponse>> RejectOwnerOffer(long serviceRequestId, long offerId, RejectServiceRequestOfferRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<AcceptServiceRequestOfferResponse>> AcceptOwnerOffer(long serviceRequestId, long offerId, AcceptServiceRequestOfferRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetServiceRequestPaymentStatusForOwnerResponse>> GetOwnerPaymentStatus(long serviceRequestId) => throw new NotImplementedException();
    public Task<AizenApiResponse<ApproveServiceRequestCompletionResponse>> ApproveOwnerCompletion(long serviceRequestId, ApproveServiceRequestCompletionRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<RejectServiceRequestCompletionResponse>> RejectOwnerCompletion(long serviceRequestId, RejectServiceRequestCompletionRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<OpenServiceRequestDisputeResponse>> OpenDispute(long serviceRequestId, OpenServiceRequestDisputeRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetOwnerDisputesResponse>> GetOwnerDisputes(int pageIndex, int pageSize, ServiceRequestDisputeStatus? status) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetDisputeCaseDetailResponse>> GetOwnerDisputeCase(long disputeId) => throw new NotImplementedException();
    public Task<AizenApiResponse<ServiceChangeOrderListDto>> GetChangeOrders(long serviceRequestId) => throw new NotImplementedException();
    public Task<AizenApiResponse<ServiceChangeOrderDto>> ApproveChangeOrder(long serviceRequestId, long changeOrderId) => throw new NotImplementedException();
    public Task<AizenApiResponse<ServiceChangeOrderDto>> RejectChangeOrder(long serviceRequestId, long changeOrderId, RejectServiceChangeOrderRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetServiceRequestPaymentStatusForOwnerResponse>> GetChangeOrderPaymentStatus(long serviceRequestId, long changeOrderId) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetMaintenanceScheduleListResponse>> GetOwnerMaintenanceSchedules(long? vesselId, bool includeInactive) => throw new NotImplementedException();
    public Task<AizenApiResponse<UpsertMaintenanceScheduleResponse>> UpsertOwnerMaintenanceSchedule(UpsertMaintenanceScheduleRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<SetMaintenanceScheduleActiveResponse>> SetOwnerMaintenanceScheduleActive(long scheduleId, SetMaintenanceScheduleActiveRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetAttachmentAccessCheckResponse>> CheckAttachmentAccess(long serviceRequestId, Guid fileId) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetServiceRequestTripForOwnerResponse>> GetOwnerTrip(long serviceRequestId) => throw new NotImplementedException();
}
