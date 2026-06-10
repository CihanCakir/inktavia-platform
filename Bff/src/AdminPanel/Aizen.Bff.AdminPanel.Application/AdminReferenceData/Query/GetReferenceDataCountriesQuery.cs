using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataCountriesQuery : AizenQuery<CountryListResult>
{
    public string UserToken { get; }

    public GetReferenceDataCountriesQuery(string userToken)
    {
        UserToken = userToken;
    }
}
