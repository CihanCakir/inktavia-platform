using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Marina;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Marina;

/// <summary>Nearest-marina lookup. Reference read (no owner scope) — passes the position through to ReferenceData
/// and projects to the cost-free mobile shape.</summary>
public sealed class GetMobileNearbyMarinasQueryHandler
    : AizenQueryHandler<GetMobileNearbyMarinasQuery, List<MobileNearbyMarinaDto>>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetMobileNearbyMarinasQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<List<MobileNearbyMarinaDto>?> Handle(
        GetMobileNearbyMarinasQuery request, CancellationToken cancellationToken)
    {
        if (request.Lat is < -90 or > 90)
            throw new AizenBusinessException("'lat' must be between -90 and 90.");
        if (request.Lng is < -180 or > 180)
            throw new AizenBusinessException("'lng' must be between -180 and 180.");

        var resp = await _referenceData.GetNearbyMarinas(request.Lat, request.Lng, request.Limit);
        var items = resp?.Body ?? new List<Aizen.Modules.ReferenceData.Abstraction.Dto.Marina.MarinaNearbyDto>();

        return items.Select(m => new MobileNearbyMarinaDto
        {
            Id = m.Id,
            Name = m.Name,
            Type = m.Type,
            Province = m.Province,
            Latitude = (double)m.Latitude,
            Longitude = (double)m.Longitude,
            DistanceMeters = m.DistanceMeters,
        }).ToList();
    }
}
