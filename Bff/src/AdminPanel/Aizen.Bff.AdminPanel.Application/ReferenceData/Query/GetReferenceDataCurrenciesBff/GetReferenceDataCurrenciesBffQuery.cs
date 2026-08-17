using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

public sealed class GetReferenceDataCurrenciesBffQuery : AizenQuery<CurrencyListResult>
{

    public GetReferenceDataCurrenciesBffQuery()
    {
    }
}
