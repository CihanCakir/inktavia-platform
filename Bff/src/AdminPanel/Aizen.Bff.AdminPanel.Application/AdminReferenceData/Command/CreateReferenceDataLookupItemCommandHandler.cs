using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup item command handler", "Proxies the create-lookup-item request to the ReferenceData module admin endpoint.")]
public sealed class CreateReferenceDataLookupItemCommandHandler
    : AizenCommandHandler<CreateReferenceDataLookupItemCommand, LookupItemDto>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public CreateReferenceDataLookupItemCommandHandler(
        IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<LookupItemDto?> Handle(
        CreateReferenceDataLookupItemCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _referenceData.CreateLookupItem(request.Request, authHeader, request.UserToken);
        return result.Body;
    }
}
