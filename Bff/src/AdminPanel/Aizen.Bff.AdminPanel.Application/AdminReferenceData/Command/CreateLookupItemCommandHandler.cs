using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup item command handler", "Forwards the create-lookup-item request to the ReferenceData admin endpoint via the cached Keycloak service token.")]
public sealed class CreateLookupItemCommandHandler
    : AizenCommandHandler<CreateLookupItemCommand, LookupItemDto>
{
    private readonly IReferenceDataRemoteCall _referenceData;

    public CreateLookupItemCommandHandler(
        IReferenceDataRemoteCall referenceData)
    {
        _referenceData = referenceData;
    }

    public override async Task<LookupItemDto?> Handle(CreateLookupItemCommand request, CancellationToken ct)
    {

        var result = await _referenceData.CreateLookupItem(request.Request);
        return result.Body;
    }
}
