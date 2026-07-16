using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetAttachmentAccessUrl;

/// <summary>
/// Access-checks a specific attachment on a specific request for the calling provider.
/// Same three-prong check as the detail query: biddable | has offer | assigned.
/// Additionally verifies the fileId is actually an attachment on the request.
/// </summary>
public sealed class GetAttachmentAccessCheckQueryHandler
    : AizenQueryHandler<GetAttachmentAccessCheckQuery, GetAttachmentAccessCheckResponse>
{
    private static readonly HashSet<ServiceRequestStatus> BiddableStatuses = new()
    {
        ServiceRequestStatus.Open,
        ServiceRequestStatus.WaitingForOffer,
        ServiceRequestStatus.OfferReceived,
    };

    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ILogger<GetAttachmentAccessCheckQueryHandler> _logger;

    public GetAttachmentAccessCheckQueryHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IServiceRequestAssignmentRepository assignmentRepository,
        IAizenInfoAccessor info,
        ILogger<GetAttachmentAccessCheckQueryHandler> logger)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _assignmentRepository = assignmentRepository;
        _info = info;
        _logger = logger;
    }

    public override async Task<GetAttachmentAccessCheckResponse?> Handle(
        GetAttachmentAccessCheckQuery request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var sr = await _srRepository.GetByIdWithDetailsAsync(request.ServiceRequestId, ct)
            ?? throw new AizenBusinessException("Service request not found.");

        // Three-prong access check
        var hasOffer = (await _offerRepository.GetByServiceRequestIdAsync(sr.Id, ct))
            .Any(o => o.ProviderProfileId == providerProfileId);
        var assignment = await _assignmentRepository.GetByServiceRequestIdAsync(sr.Id, ct);
        var isAssignedToMe = assignment is not null && assignment.ProviderProfileId == providerProfileId;
        var maySee = BiddableStatuses.Contains(sr.Status) || hasOffer || isAssignedToMe;

        if (!maySee)
        {
            _logger.LogWarning(
                "Provider {ProviderProfileId} tried to access attachment on SR {ServiceRequestId} with no relationship.",
                providerProfileId, sr.Id);
            throw new AizenBusinessException("Service request not found.");
        }

        // Verify fileId is an attachment on this request
        var attachment = sr.Attachments.FirstOrDefault(a => a.FileId == request.FileId && !a.IsDeleted);
        if (attachment is null)
        {
            _logger.LogWarning(
                "Provider {ProviderProfileId} requested read-url for fileId {FileId} which is not an attachment on SR {ServiceRequestId}.",
                providerProfileId, request.FileId, sr.Id);
            throw new AizenBusinessException("Service request not found.");
        }

        return new GetAttachmentAccessCheckResponse(attachment.FileId, true);
    }
}
