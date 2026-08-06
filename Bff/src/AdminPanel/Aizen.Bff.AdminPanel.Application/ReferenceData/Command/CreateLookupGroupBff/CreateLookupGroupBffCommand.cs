using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

namespace Aizen.Bff.AdminPanel.Application.ReferenceData.Command;

[DocumentationInfo("Create lookup group command", "Carries a CreateLookupGroupRequest to the BFF handler that forwards it to the ReferenceData admin endpoint.")]
public sealed class CreateLookupGroupBffCommand : AizenCommand<LookupGroupDto>
{
    public CreateLookupGroupRequest Request { get; }
    public CreateLookupGroupBffCommand(CreateLookupGroupRequest request)
    {
        Request = request;
    }
}
