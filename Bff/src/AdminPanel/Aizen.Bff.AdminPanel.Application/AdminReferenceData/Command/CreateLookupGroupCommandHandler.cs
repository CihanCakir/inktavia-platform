using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup group command handler", "Forwards the create-lookup-group request to the ReferenceData admin endpoint via the cached Keycloak service token.")]
public sealed class CreateLookupGroupCommandHandler
    : AizenCommandHandler<CreateLookupGroupCommand, LookupGroupDto>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public CreateLookupGroupCommandHandler(
        IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<LookupGroupDto?> Handle(CreateLookupGroupCommand request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _referenceData.CreateLookupGroup(request.Request, authHeader, request.UserToken);
        return result.Body;
    }
}
