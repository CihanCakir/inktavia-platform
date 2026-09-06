using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;

namespace Aizen.Modules.ServiceRequest.Application.Services.Trip;

/// <summary>Shared terminal-transition logic for Arrive/Cancel: guard → compute the trail summary → set terminal
/// status + summary → PURGE the raw trail (privacy) → fan out. Arrive and Cancel differ only in the terminal state.</summary>
public sealed class TripFinalizeService
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IServiceRequestTripRepository _tripRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly ServiceRequestDbContext _db;

    public TripFinalizeService(
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

    public async Task<TripActionResponse> FinalizeAsync(long serviceRequestId, bool arrived, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        var sr = await _srRepository.GetByIdAsync(serviceRequestId, ct);
        var assignment = await _assignmentRepository.GetByServiceRequestIdAsync(serviceRequestId, ct);
        TripAccess.EnsureAssignedProviderOnTrackableJob(providerProfileId, sr, assignment);

        var trip = await _tripRepository.GetByServiceRequestIdAsync(serviceRequestId, ct)
            ?? throw new AizenBusinessException("TRIP_NOT_ENROUTE");

        // Summary from the trail BEFORE purge (guards EnRoute via the domain call below).
        var positions = await _tripRepository.GetPositionsAsync(trip.Id, ct);
        var (totalKm, durationSeconds) = TripMath.ComputeSummary(positions);

        if (arrived) trip.Arrive(); else trip.Cancel(); // throws TRIP_NOT_ENROUTE if already terminal
        trip.SetSummary(totalKm, durationSeconds);
        _tripRepository.Update(trip);
        await _db.SaveChangesAsync(ct);

        // Privacy: hard-purge the raw trail once the summary is stored.
        await _tripRepository.PurgePositionsAsync(trip.Id, ct);

        await _messagePublisher.PublishAsync(new TripRealtimeMessage
        {
            ServiceRequestId = sr!.Id,
            OwnerUserId = sr.OwnerUserId,
            ProviderProfileId = trip.ProviderProfileId,
            EventType = arrived ? TripEventType.Arrived : TripEventType.Cancelled,
            Status = trip.Status,
            Latitude = trip.LastLatitude,
            Longitude = trip.LastLongitude,
            Heading = trip.LastHeading,
            PingAt = trip.LastPingAt,
            EtaMinutes = null,
            StartedAt = trip.StartedAt,
            OccurredAt = DateTimeOffset.UtcNow,
        }, ct);

        return new TripActionResponse(trip.ToDto());
    }
}
