using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup item command handler", "Forwards the create-lookup-item request to the ReferenceData admin endpoint via the cached Keycloak service token.")]
public sealed class CreateLookupItemCommandHandler
    : AizenCommandHandler<CreateLookupItemCommand, LookupItemDto>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public CreateLookupItemCommandHandler(
        IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<LookupItemDto?> Handle(CreateLookupItemCommand request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _referenceData.CreateLookupItem(request.Request, authHeader, request.UserToken);
        return result.Body;
    }
}
