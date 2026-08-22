using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Bff.MarineProvider.Application.Location;

public sealed class GetCountriesBffQueryHandler
    : AizenQueryHandler<GetCountriesBffQuery, List<CountryDto>>
{
    private readonly IReferenceDataRemoteCall _ref;

    public GetCountriesBffQueryHandler(IReferenceDataRemoteCall r) => _ref = r;

    public override async Task<List<CountryDto>?> Handle(GetCountriesBffQuery q, CancellationToken ct)
    {
        return (await _ref.GetCountries()).Body;
    }
}
