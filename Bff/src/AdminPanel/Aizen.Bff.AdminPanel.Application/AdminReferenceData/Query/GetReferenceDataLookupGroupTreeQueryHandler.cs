using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupGroupTree handler", "Forwards admin lookup group tree request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupGroupTreeQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupGroupTreeQuery, LookupGroupTreeResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetReferenceDataLookupGroupTreeQueryHandler(IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<LookupGroupTreeResult> Handle(GetReferenceDataLookupGroupTreeQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var response = await _referenceData.GetLookupGroupTree(authHeader, request.UserToken);
        return response.Body ?? new LookupGroupTreeResult();
    }
}
