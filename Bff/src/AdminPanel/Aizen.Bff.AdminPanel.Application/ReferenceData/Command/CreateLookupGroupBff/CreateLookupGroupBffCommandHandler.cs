using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Command;

[DocumentationInfo("Create lookup group command handler", "Forwards the create-lookup-group request to the ReferenceData admin endpoint via the cached Keycloak service token.")]
public sealed class CreateLookupGroupBffCommandHandler
    : AizenCommandHandler<CreateLookupGroupBffCommand, LookupGroupDto>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public CreateLookupGroupBffCommandHandler(
        IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupGroupDto?> Handle(CreateLookupGroupBffCommand request, CancellationToken ct)
    {

        var result = await _referenceData.CreateLookupGroup(request.Request);
        return result.Body;
    }
}
