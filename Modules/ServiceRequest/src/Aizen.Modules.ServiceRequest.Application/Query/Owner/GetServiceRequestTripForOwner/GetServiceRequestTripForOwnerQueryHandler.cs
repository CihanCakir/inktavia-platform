using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Application.Services.Trip;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner;

[DocumentationInfo("Get service request trip for owner query handler", "Ownership-checked; returns the live trip (with a coarse ETA while EnRoute) or null.")]
public sealed class GetServiceRequestTripForOwnerQueryHandler
    : AizenQueryHandler<GetServiceRequestTripForOwnerQuery, GetServiceRequestTripForOwnerResponse>
{
    private const int RecentSpeedSampleSize = 6;

    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestTripRepository _tripRepository;
    private readonly IAizenInfoAccessor _info;

    public GetServiceRequestTripForOwnerQueryHandler(
        IServiceRequestRepository srRepository, IServiceRequestTripRepository tripRepository, IAizenInfoAccessor info)
    {
        _srRepository = srRepository;
        _tripRepository = tripRepository;
        _info = info;
    }

    public override async Task<GetServiceRequestTripForOwnerResponse?> Handle(
        GetServiceRequestTripForOwnerQuery request, CancellationToken ct)
    {
        var ownerUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, ct);
        if (sr is null || sr.OwnerUserId != ownerUserId)
            throw new AizenBusinessException("Service request not found."); // never leak existence

        var trip = await _tripRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, ct);
        if (trip is null)
            return new GetServiceRequestTripForOwnerResponse(null);

        double? eta = null;
        if (trip.IsEnRoute)
        {
            var recent = await _tripRepository.GetRecentPositionsAsync(trip.Id, RecentSpeedSampleSize, ct);
            eta = TripMath.ComputeEtaMinutes(trip.LastLatitude, trip.LastLongitude, sr.LocationLatitude, sr.LocationLongitude, recent);
        }

        return new GetServiceRequestTripForOwnerResponse(trip.ToDto(eta));
    }
}
