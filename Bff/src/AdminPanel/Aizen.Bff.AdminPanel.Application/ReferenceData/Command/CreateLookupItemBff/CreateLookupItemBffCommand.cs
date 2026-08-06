using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Command;

[DocumentationInfo("Create lookup item command", "Carries a CreateLookupItemRequest to the BFF handler that forwards it to the ReferenceData admin endpoint.")]
public sealed class CreateLookupItemBffCommand : AizenCommand<LookupItemDto>
{
    public CreateLookupItemRequest Request { get; }
    public CreateLookupItemBffCommand(CreateLookupItemRequest request)
    {
        Request = request;
    }
}
