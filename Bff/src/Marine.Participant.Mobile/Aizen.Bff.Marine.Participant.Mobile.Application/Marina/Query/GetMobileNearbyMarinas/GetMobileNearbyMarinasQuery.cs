using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Marina;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Marina;

/// <summary>GET /api/v1/mobile/marinas/nearby?lat=&amp;lng= — nearest marinas to a device position.</summary>
public sealed class GetMobileNearbyMarinasQuery : AizenQuery<List<MobileNearbyMarinaDto>>
{
    public GetMobileNearbyMarinasQuery(double lat, double lng, int limit = 10)
    {
        Lat = lat;
        Lng = lng;
        Limit = limit;
    }

    public double Lat { get; }
    public double Lng { get; }
    public int Limit { get; }
}
