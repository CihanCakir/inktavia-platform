using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Commands;

public sealed class CreateLookupItemCommand : AizenCommand<LookupItemDto>
{
    public CreateLookupItemRequest Request { get; }

    public CreateLookupItemCommand(CreateLookupItemRequest request)
    {
        Request = request;
    }
}
