using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

public sealed class GetReferenceDataCitiesBffQuery : AizenQuery<CityListResult>
{
    public string CountryCode { get; }

    public GetReferenceDataCitiesBffQuery(string countryCode)
    {
        CountryCode = countryCode;
    }
}
