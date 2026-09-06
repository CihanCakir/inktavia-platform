using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Application.Services.Trip;
using Aizen.Modules.ServiceRequest.Domain.Entities.Trip;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;

namespace Aizen.Modules.ServiceRequest.Application.Command.Trip;

[DocumentationInfo("Start trip command handler", "Opens/re-arms the SR's trip as EnRoute (assigned provider only), publishes TripStarted realtime + owner notification.")]
public sealed class StartTripCommandHandler : AizenCommandHandler<StartTripCommand, TripActionResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IServiceRequestTripRepository _tripRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly ServiceRequestDbContext _db;

    public StartTripCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestAssignmentRepository assignmentRepository,
        IServiceRequestTripRepository tripRepository,
        IAizenInfoAccessor info,
        IAizenMessagePublisher messagePublisher,
        ServiceRequestDbContext db)
    {
        _srRepository = srRepository;
        _assignmentRepository = assignmentRepository;
        _tripRepository = tripRepository;
        _info = info;
        _messagePublisher = messagePublisher;
        _db = db;
    }

    public override async Task<TripActionResponse?> Handle(StartTripCommand request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, ct);
        var assignment = await _assignmentRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, ct);
        TripAccess.EnsureAssignedProviderOnTrackableJob(providerProfileId, sr, assignment);

        var trip = await _tripRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, ct);
        if (trip is null)
        {
            trip = ServiceRequestTripEntity.Start(request.ServiceRequestId, providerProfileId);
            await _tripRepository.AddAsync(trip, ct);
        }
        else
        {
            // Re-arm a terminal trip (throws TRIP_ALREADY_ACTIVE if still EnRoute — no double-start).
            trip.RestartEnRoute(providerProfileId);
            _tripRepository.Update(trip);
        }

        await _db.SaveChangesAsync(ct); // assign the trip id before publishing

        await PublishAsync(sr!, trip, TripEventType.Started, TripStatus.EnRoute, eta: null, ct);
        await _messagePublisher.PublishAsync(new ServiceRequestTripStartedMessage
        {
            ServiceRequestId = sr!.Id,
            RequestCode = sr.RequestCode,
            ProviderProfileId = providerProfileId,
            OwnerUserId = sr.OwnerUserId,
            OccurredAt = DateTimeOffset.UtcNow,
        }, ct);

        return new TripActionResponse(trip.ToDto());
    }

    private Task PublishAsync(
        Domain.Entities.ServiceRequest.ServiceRequestEntity sr, ServiceRequestTripEntity trip,
        TripEventType eventType, TripStatus status, double? eta, CancellationToken ct)
        => _messagePublisher.PublishAsync(new TripRealtimeMessage
        {
            ServiceRequestId = sr.Id,
            OwnerUserId = sr.OwnerUserId,
            ProviderProfileId = trip.ProviderProfileId,
            EventType = eventType,
            Status = status,
            Latitude = trip.LastLatitude,
            Longitude = trip.LastLongitude,
            Heading = trip.LastHeading,
            PingAt = trip.LastPingAt,
            EtaMinutes = eta,
            StartedAt = trip.StartedAt,
            OccurredAt = DateTimeOffset.UtcNow,
        }, ct);
}
