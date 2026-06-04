using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

public sealed class GetReferenceDataCurrenciesQuery : AizenQuery<CurrencyListResult>
{
    public string Authorization { get; }
    public string UserToken { get; }

    public GetReferenceDataCurrenciesQuery(string authorization, string userToken)
    {
        Authorization = authorization;
        UserToken = userToken;
    }
}
