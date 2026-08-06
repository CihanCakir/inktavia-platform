using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

[DocumentationInfo("GetReferenceDataCities handler", "Forwards admin city list request to the ReferenceData module.")]
public sealed class GetReferenceDataCitiesBffQueryHandler
    : AizenQueryHandler<GetReferenceDataCitiesBffQuery, CityListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataCitiesBffQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<CityListResult> Handle(GetReferenceDataCitiesBffQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetCities(
            request.CountryCode);
        return new CityListResult { Items = response.Body?.ToList() };
    }
}
