using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Command;

[DocumentationInfo("Create lookup item command handler", "Forwards the create-lookup-item request to the ReferenceData admin endpoint via the cached Keycloak service token.")]
public sealed class CreateLookupItemBffCommandHandler
    : AizenCommandHandler<CreateLookupItemBffCommand, LookupItemDto>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public CreateLookupItemBffCommandHandler(
        IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupItemDto?> Handle(CreateLookupItemBffCommand request, CancellationToken ct)
    {

        var result = await _referenceData.CreateLookupItem(request.Request);
        return result.Body;
    }
}
