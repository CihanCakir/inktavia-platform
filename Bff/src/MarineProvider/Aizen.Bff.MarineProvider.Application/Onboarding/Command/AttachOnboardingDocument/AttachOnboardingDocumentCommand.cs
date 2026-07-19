using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Onboarding;

public sealed class AttachOnboardingDocumentCommand : AizenCommand<AttachDocumentBffResponse>
{
    public Guid FileId { get; set; }
    public string DocumentType { get; set; } = default!;
    public string? Issuer { get; set; }
}
