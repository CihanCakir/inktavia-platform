using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RemoveProviderDocument;

public sealed class RemoveProviderDocumentCommand : AizenCommand<RemoveProviderDocumentResponse>
{
    public long ProfileId { get; set; }
    public Guid FileId { get; set; }
}
