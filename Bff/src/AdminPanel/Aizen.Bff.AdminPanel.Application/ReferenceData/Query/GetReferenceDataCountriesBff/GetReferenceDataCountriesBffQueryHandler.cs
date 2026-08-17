using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

[DocumentationInfo("GetReferenceDataCountries handler", "Forwards admin country list request to the ReferenceData module.")]
public sealed class GetReferenceDataCountriesBffQueryHandler
    : AizenQueryHandler<GetReferenceDataCountriesBffQuery, CountryListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataCountriesBffQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<CountryListResult> Handle(GetReferenceDataCountriesBffQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetCountries();
        return new CountryListResult { Items = response.Body?.ToList() };
    }
}
