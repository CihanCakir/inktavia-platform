using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup group command handler", "Proxies the create-lookup-group request to the ReferenceData module admin endpoint.")]
public sealed class CreateReferenceDataLookupGroupCommandHandler
    : AizenCommandHandler<CreateReferenceDataLookupGroupCommand, LookupGroupDto>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public CreateReferenceDataLookupGroupCommandHandler(
        IReferenceDataAdminBffRemoteCall referenceData,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _referenceData = referenceData;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<LookupGroupDto?> Handle(
        CreateReferenceDataLookupGroupCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var result = await _referenceData.CreateLookupGroup(request.Request, authHeader, request.UserToken);
        return result.Body;
    }
}
