using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup group command", "Carries a CreateLookupGroupRequest to the BFF handler that forwards it to the ReferenceData admin endpoint.")]
public sealed class CreateLookupGroupCommand : AizenCommand<LookupGroupDto>
{
    public CreateLookupGroupRequest Request { get; }
    public CreateLookupGroupCommand(CreateLookupGroupRequest request)
    {
        Request = request;
    }
}
