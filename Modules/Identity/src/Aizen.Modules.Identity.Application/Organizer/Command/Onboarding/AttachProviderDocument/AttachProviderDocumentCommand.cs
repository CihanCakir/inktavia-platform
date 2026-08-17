using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.AttachProviderDocument;

public sealed class AttachProviderDocumentCommand : AizenCommand<AttachProviderDocumentResponse>
{
    public long ProfileId { get; set; }
    public long UserId { get; set; }
    public Guid FileId { get; set; }
    public string DocumentType { get; set; } = default!;
    public string? Issuer { get; set; }
}
