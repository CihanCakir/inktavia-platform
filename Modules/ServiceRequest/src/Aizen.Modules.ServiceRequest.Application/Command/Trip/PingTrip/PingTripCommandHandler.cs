using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
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

[DocumentationInfo("Ping trip command handler", "Records a provider position (assigned provider, EnRoute only), server-side throttled (<3s ignored); fans out TripLocation with a coarse ETA.")]
public sealed class PingTripCommandHandler : AizenCommandHandler<PingTripCommand, TripActionResponse>
{
    private const int RecentSpeedSampleSize = 6;

    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IServiceRequestTripRepository _tripRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly ServiceRequestDbContext _db;

    public PingTripCommandHandler(
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

    public override async Task<TripActionResponse?> Handle(PingTripCommand request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, ct);
        var assignment = await _assignmentRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, ct);
        TripAccess.EnsureAssignedProviderOnTrackableJob(providerProfileId, sr, assignment);

        var trip = await _tripRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, ct)
            ?? throw new AizenBusinessException("TRIP_NOT_ENROUTE");
        if (!trip.IsEnRoute)
            throw new AizenBusinessException("TRIP_NOT_ENROUTE");

        var now = DateTime.UtcNow;
        // Server-side throttle: ignore pings arriving <3s after the last accepted one. No write, no fan-out.
        if (TripMath.ShouldThrottle(trip.LastPingAt, now))
            return new TripActionResponse(trip.ToDto());

        var r = request.Request;
        trip.Ping(r.Latitude, r.Longitude, r.Heading, now); // guards EnRoute
        _tripRepository.Update(trip);
        await _tripRepository.AddPositionAsync(
            ServiceRequestTripPositionEntity.Create(trip.Id, r.Latitude, r.Longitude, r.Heading, now), ct);
        await _db.SaveChangesAsync(ct);

        // ETA from the last position + recent average speed (now includes the just-saved ping).
        var recent = await _tripRepository.GetRecentPositionsAsync(trip.Id, RecentSpeedSampleSize, ct);
        var eta = TripMath.ComputeEtaMinutes(trip.LastLatitude, trip.LastLongitude, sr!.LocationLatitude, sr.LocationLongitude, recent);

        await _messagePublisher.PublishAsync(new TripRealtimeMessage
        {
            ServiceRequestId = sr.Id,
            OwnerUserId = sr.OwnerUserId,
            ProviderProfileId = trip.ProviderProfileId,
            EventType = TripEventType.Location,
            Status = trip.Status,
            Latitude = trip.LastLatitude,
            Longitude = trip.LastLongitude,
            Heading = trip.LastHeading,
            PingAt = trip.LastPingAt,
            EtaMinutes = eta,
            StartedAt = trip.StartedAt,
            OccurredAt = DateTimeOffset.UtcNow,
        }, ct);

        return new TripActionResponse(trip.ToDto(eta));
    }
}
