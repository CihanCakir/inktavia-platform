using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataCountries handler", "Forwards admin country list request to the ReferenceData module.")]
public sealed class GetReferenceDataCountriesQueryHandler
    : AizenQueryHandler<GetReferenceDataCountriesQuery, CountryListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    public GetReferenceDataCountriesQueryHandler(IReferenceDataAdminBffRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<CountryListResult> Handle(GetReferenceDataCountriesQuery request, CancellationToken ct)
    {
        var response = await _referenceData.GetCountries(request.Authorization, request.UserToken);
        return response.Body ?? new CountryListResult();
    }
}
