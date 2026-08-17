using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Bff.MarineProvider.Application.Location;

public sealed class GetCitiesBffQueryHandler
    : AizenQueryHandler<GetCitiesBffQuery, List<CityDto>>
{
    private readonly IReferenceDataRemoteCall _ref;

    public GetCitiesBffQueryHandler(IReferenceDataRemoteCall r) => _ref = r;

    public override async Task<List<CityDto>?> Handle(GetCitiesBffQuery q, CancellationToken ct)
    {
        return (await _ref.GetCitiesByCountry(q.Country)).Body;
    }
}
