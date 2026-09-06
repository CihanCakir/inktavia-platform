using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDiscovery;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderServiceRequestDetail;

/// <summary>
/// Service request detail for a provider, with an explicit access check.
///
/// Without this, a provider could read ANY service request by guessing an id — including private details of
/// requests belonging to other providers' customers. A provider may READ a request only when one of these is true:
///
///   1. it is currently biddable (the same states the "open" list exposes), or
///   2. it reached a terminal state (Completed/Cancelled/Expired/Closed) AFTER having been published — i.e. it was
///      already broadcast to every provider in its city, so a terminal read leaks nothing new. This is the
///      deep-link / refresh case: a provider opens a card for a request that has since been cancelled and must get a
///      readable terminal payload (200, status = Cancelled) rather than a 400. Offer affordances suppress themselves
///      because the status is no longer biddable, or
///   3. the provider has an offer on it, or
///   4. it is assigned to the provider.
///
/// This is a READ-ONLY relaxation: write paths (submit/update offer, approve change order, …) keep their own
/// terminal-state guards untouched. A never-published Draft that was cancelled stays private (no relationship, not
/// published → still rejected). Anything else is a rejection, not an empty result — an empty result would tell the
/// caller the request exists.
/// </summary>
[DocumentationInfo("Get provider service request detail handler", "Returns one service request if the calling provider may see it; rejects otherwise.")]
public sealed class GetProviderServiceRequestDetailQueryHandler
    : AizenQueryHandler<GetProviderServiceRequestDetailQuery, GetProviderServiceRequestDetailResponse>
{
    private static readonly HashSet<ServiceRequestStatus> BiddableStatuses = new()
    {
        ServiceRequestStatus.Open,
        ServiceRequestStatus.WaitingForOffer,
        ServiceRequestStatus.OfferReceived,
    };

    // Terminal lifecycle states. A request that reached one of these AFTER being published was already public to
    // city providers, so it stays readable (read-only) — this closes the terminal-state 400 on deep-link/refresh.
    private static readonly HashSet<ServiceRequestStatus> TerminalStatuses = new()
    {
        ServiceRequestStatus.Completed,
        ServiceRequestStatus.Cancelled,
        ServiceRequestStatus.Expired,
        ServiceRequestStatus.Closed,
    };

    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ILogger<GetProviderServiceRequestDetailQueryHandler> _logger;

    public GetProviderServiceRequestDetailQueryHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IServiceRequestAssignmentRepository assignmentRepository,
        IAizenInfoAccessor info,
        ILogger<GetProviderServiceRequestDetailQueryHandler> logger)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _assignmentRepository = assignmentRepository;
        _info = info;
        _logger = logger;
    }

    public override async Task<GetProviderServiceRequestDetailResponse?> Handle(
        GetProviderServiceRequestDetailQuery request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var sr = await _srRepository.GetByIdWithDetailsAsync(request.ServiceRequestId, ct)
            ?? throw new AizenBusinessException("Service request not found.");

        var hasOffer = (await _offerRepository.GetByServiceRequestIdAsync(sr.Id, ct))
            .Any(o => o.ProviderProfileId == providerProfileId);

        var assignment = await _assignmentRepository.GetByServiceRequestIdAsync(sr.Id, ct);
        var isAssignedToMe = assignment is not null && assignment.ProviderProfileId == providerProfileId;

        // A terminal request that was published is already public to city providers → readable read-only. This is
        // the fix for the terminal-state 400 on deep-link/refresh; writes keep their own guards.
        var isPublishedTerminal = TerminalStatuses.Contains(sr.Status) && sr.PublishedAt is not null;

        var maySee = BiddableStatuses.Contains(sr.Status) || isPublishedTerminal || hasOffer || isAssignedToMe;

        if (!maySee)
        {
            _logger.LogWarning(
                "Provider {ProviderProfileId} tried to read service request {ServiceRequestId} ({Status}) with no relationship to it.",
                providerProfileId, sr.Id, sr.Status);
            throw new AizenBusinessException("Service request not found.");
        }

        var detail = sr.ToProviderDetailDto(providerProfileId);

        // Surface the assignment id when this request is assigned to the caller, so the detail page can link the
        // accepted+assigned state straight to its Job.
        detail.AssignmentId = isAssignedToMe ? assignment!.Id : null;

        // Compute distance from the provider's location (exact coords stay in the module)
        if (request.CenterLatitude.HasValue && request.CenterLongitude.HasValue
            && sr.LocationLatitude.HasValue && sr.LocationLongitude.HasValue)
        {
            detail.Request.DistanceKm = Math.Round(
                GeoHelper.HaversineKm(
                    request.CenterLatitude.Value, request.CenterLongitude.Value,
                    sr.LocationLatitude.Value, sr.LocationLongitude.Value), 1);
        }

        return new GetProviderServiceRequestDetailResponse(detail);
    }
}
