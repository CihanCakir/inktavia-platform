using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataCurrencies handler", "Forwards admin currency list request to the ReferenceData module.")]
public sealed class GetReferenceDataCurrenciesQueryHandler
    : AizenQueryHandler<GetReferenceDataCurrenciesQuery, CurrencyListResult>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public GetReferenceDataCurrenciesQueryHandler(IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<CurrencyListResult> Handle(GetReferenceDataCurrenciesQuery request, CancellationToken ct)
    {

        var response = await _referenceData.GetCurrencies();
        return new CurrencyListResult { Items = response.Body?.ToList() };
    }
}
