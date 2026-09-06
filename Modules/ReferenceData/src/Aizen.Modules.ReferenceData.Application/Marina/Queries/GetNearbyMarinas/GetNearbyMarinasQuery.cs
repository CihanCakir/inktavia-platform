using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;

namespace Aizen.Modules.ReferenceData.Application.Marina.Queries;

public sealed class GetNearbyMarinasQuery : AizenQuery<IReadOnlyList<MarinaNearbyDto>>
{
    public double Latitude { get; }
    public double Longitude { get; }
    public int Limit { get; }

    public GetNearbyMarinasQuery(double latitude, double longitude, int limit = 10)
    {
        Latitude = latitude;
        Longitude = longitude;
        Limit = limit;
    }
}
