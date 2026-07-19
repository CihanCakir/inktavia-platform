using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Onboarding;

public sealed class DeleteOnboardingDocumentCommand : AizenCommand<DeleteDocumentBffResponse>
{
    public Guid FileId { get; set; }
}
