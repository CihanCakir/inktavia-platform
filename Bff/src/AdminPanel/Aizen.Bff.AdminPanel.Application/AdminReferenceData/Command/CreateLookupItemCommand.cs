using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup item command", "Carries a CreateLookupItemRequest to the BFF handler that forwards it to the ReferenceData admin endpoint.")]
public sealed class CreateLookupItemCommand : AizenCommand<LookupItemDto>
{
    public CreateLookupItemRequest Request { get; }
    public CreateLookupItemCommand(CreateLookupItemRequest request)
    {
        Request = request;
    }
}
