using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Query;

[DocumentationInfo("GetReferenceDataCurrencies handler", "Forwards admin currency list request to the ReferenceData module.")]
public sealed class GetReferenceDataCurrenciesBffQueryHandler
    : AizenQueryHandler<GetReferenceDataCurrenciesBffQuery, CurrencyListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataCurrenciesBffQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<CurrencyListResult> Handle(GetReferenceDataCurrenciesBffQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetCurrencies();
        return new CurrencyListResult { Items = response.Body?.ToList() };
    }
}
