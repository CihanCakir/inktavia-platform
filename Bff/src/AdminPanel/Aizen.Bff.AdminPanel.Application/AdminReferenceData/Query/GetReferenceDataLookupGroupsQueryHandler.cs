using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Query;

[DocumentationInfo("GetReferenceDataLookupGroups handler", "Forwards admin lookup group list request to the ReferenceData module.")]
public sealed class GetReferenceDataLookupGroupsQueryHandler
    : AizenQueryHandler<GetReferenceDataLookupGroupsQuery, LookupGroupListResult>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetReferenceDataLookupGroupsQueryHandler(IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<LookupGroupListResult> Handle(GetReferenceDataLookupGroupsQuery request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var response = await _referenceData.GetLookupGroups(authHeader, request.UserToken);
        return response.Body ?? new LookupGroupListResult();
    }
}
