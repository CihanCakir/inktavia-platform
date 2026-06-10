using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataCurrencies handler", "Forwards admin currency list request to the ReferenceData module.")]
public sealed class GetReferenceDataCurrenciesQueryHandler
    : AizenQueryHandler<GetReferenceDataCurrenciesQuery, CurrencyListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetReferenceDataCurrenciesQueryHandler(IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<CurrencyListResult> Handle(GetReferenceDataCurrenciesQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var response = await _referenceData.GetCurrencies(authHeader, request.UserToken);
        return response.Body ?? new CurrencyListResult();
    }
}
