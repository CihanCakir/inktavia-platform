using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup group command handler", "Forwards the create-lookup-group request to the ReferenceData admin endpoint via the cached Keycloak service token.")]
public sealed class CreateLookupGroupCommandHandler
    : AizenCommandHandler<CreateLookupGroupCommand, LookupGroupDto>
{
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    public CreateLookupGroupCommandHandler(
        IReferenceDataAdminBffRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupGroupDto?> Handle(CreateLookupGroupCommand request, CancellationToken ct)
    {

        var result = await _referenceData.CreateLookupGroup(request.Request);
        return result.Body;
    }
}
