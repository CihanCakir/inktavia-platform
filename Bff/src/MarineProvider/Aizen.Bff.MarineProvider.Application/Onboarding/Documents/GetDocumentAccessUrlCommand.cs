using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.Documents;

public sealed class GetDocumentAccessUrlCommand : AizenCommand<DocumentAccessUrlBffResponse>
{
    public Guid FileId { get; set; }
}
