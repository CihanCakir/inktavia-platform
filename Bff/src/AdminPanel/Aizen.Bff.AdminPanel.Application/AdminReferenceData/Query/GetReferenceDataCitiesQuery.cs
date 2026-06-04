using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataCitiesQuery : AizenQuery<CityListResult>
{
    public long? CountryId { get; }
    public string Authorization { get; }
    public string UserToken { get; }

    public GetReferenceDataCitiesQuery(long? countryId, string authorization, string userToken)
    {
        CountryId = countryId;
        Authorization = authorization;
        UserToken = userToken;
    }
}
