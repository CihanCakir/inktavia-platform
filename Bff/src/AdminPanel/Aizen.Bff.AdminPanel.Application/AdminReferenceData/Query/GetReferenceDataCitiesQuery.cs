using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataCitiesQuery : AizenQuery<CityListResult>
{
    public string CountryCode { get; }
    public string UserToken { get; }

    public GetReferenceDataCitiesQuery(string countryCode, string userToken)
    {
        CountryCode = countryCode;
        UserToken = userToken;
    }
}
