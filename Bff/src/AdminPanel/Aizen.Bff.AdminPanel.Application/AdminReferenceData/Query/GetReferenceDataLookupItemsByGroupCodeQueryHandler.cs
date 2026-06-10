using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupItemsByGroupCode handler", "Forwards admin lookup items by group code request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupItemsByGroupCodeQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupItemsByGroupCodeQuery, LookupItemListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetReferenceDataLookupItemsByGroupCodeQueryHandler(IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<LookupItemListResult> Handle(GetReferenceDataLookupItemsByGroupCodeQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var response = await _referenceData.GetLookupItemsByGroupCode(
            request.GroupCode, authHeader, request.UserToken);
        return response.Body ?? new LookupItemListResult();
    }
}
