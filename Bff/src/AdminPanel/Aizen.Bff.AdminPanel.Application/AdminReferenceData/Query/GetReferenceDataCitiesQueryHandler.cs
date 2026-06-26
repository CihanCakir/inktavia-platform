using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataCities handler", "Forwards admin city list request to the ReferenceData module.")]
public sealed class GetReferenceDataCitiesQueryHandler
    : AizenQueryHandler<GetReferenceDataCitiesQuery, CityListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    public GetReferenceDataCitiesQueryHandler(IReferenceDataAdminBffRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<CityListResult> Handle(GetReferenceDataCitiesQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetCities(
            request.CountryCode);
        return new CityListResult { Items = response.Body?.ToList() };
    }
}
